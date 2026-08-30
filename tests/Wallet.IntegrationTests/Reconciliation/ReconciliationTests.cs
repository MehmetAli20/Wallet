using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Transfers;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.Reconciliation;
using Wallet.Infrastructure.Persistence.Repositories.Accounts;

namespace Wallet.IntegrationTests.Reconciliation
{
    public class ReconciliationTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;
        public ReconciliationTests(PostgresFixture fixture) => _fixture = fixture;

        private async Task<(Guid source, Guid destination, Guid groupId)> SeedTransferAsync(string currency)
        {
            var sourceId = Guid.NewGuid();
            var destinationId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var repository = new AccountRepository(context);

            var source = new Account(sourceId, Guid.NewGuid(), currency);
            var destination = new Account(destinationId, Guid.NewGuid(), currency);

            await repository.AddAsync(source);
            await repository.AddAsync(destination);
            new TransferService().Settle(source, destination, new Money(40m, currency), groupId);
            await new UnitOfWork(context).SaveChangesAsync();

            return (sourceId, destinationId, groupId);
        }

        [Fact]
        public async Task HealthyLedger_IsBalanced()
        {
            var (source, destination, _) = await SeedTransferAsync("USD");

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var repository = new ReconciliationRepository(context);

            var discrepancies = await repository.GetAccountDiscrepanciesAsync();
            discrepancies.Should().NotContain(d => d.AccountId == source || d.AccountId == destination);

            var totals = await repository.GetLedgerTotalsAsync();
            totals.Single(t => t.Currency == "USD").IsBalanced.Should().BeTrue();

            var positions = await repository.GetNetPositionsAsync();
            positions.Single(p => p.Currency == "USD").IsBalanced.Should().BeTrue();
        }

        [Fact]
        public async Task BalanceDriftedFromLedger_IsDetected()
        {
            var (sourceId, _, _) = await SeedTransferAsync("GBP");

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            await context.Database.ExecuteSqlAsync(
                $"UPDATE \"Accounts\" SET \"Balance\" = 999 WHERE \"Id\" = {sourceId}");

            var discrepancies = await new ReconciliationRepository(context).GetAccountDiscrepanciesAsync();

            discrepancies.Should().ContainSingle(d => d.AccountId == sourceId);
            discrepancies.Single(d => d.AccountId == sourceId).Balance.Should().Be(999m);
        }

        [Fact]
        public async Task PositionsNotNettingToZero_IsDetected()
        {
            var (sourceId, _, _) = await SeedTransferAsync("EUR");

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            await context.Database.ExecuteSqlAsync(
                $"UPDATE \"Accounts\" SET \"Balance\" = \"Balance\" + 500 WHERE \"Id\" = {sourceId}");

            var positions = await new ReconciliationRepository(context).GetNetPositionsAsync();

            positions.Should().Contain(p => p.Currency == "EUR" && !p.IsBalanced);
        }

        [Fact]
        public async Task Reconciliation_IsGlobal_EvenInAUserContext()
        {
            await SeedTransferAsync("TRY");

            await using var context = _fixture.CreateContext(TestCurrentUser.For(Guid.NewGuid()));
            var positions = await new ReconciliationRepository(context).GetNetPositionsAsync();

            positions.Should().NotBeEmpty();
        }

        [Fact]
        public async Task HealthyContext_NetsToZero()
        {
            var (_, _, groupId) = await SeedTransferAsync("SEK");

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var contexts = await new ReconciliationRepository(context).GetContextBalancesAsync();

            contexts.Single(c => c.GroupId == groupId).IsBalanced.Should().BeTrue();
        }

        [Fact]
        public async Task UnbalancedContext_IsDetected()
        {
            var (sourceId, _, groupId) = await SeedTransferAsync("NOK");

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var account = await new AccountRepository(context).GetByIdAsync(sourceId);

                context.Set<LedgerEntry>().Add(new LedgerEntry(
                    Guid.NewGuid(), sourceId, account!.OwnerId, groupId, Guid.NewGuid(),
                    LedgerEntryType.Credit, new Money(25m, "NOK"), DateTimeOffset.UtcNow, 99));

                await new UnitOfWork(context).SaveChangesAsync();
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var contexts = await new ReconciliationRepository(context).GetContextBalancesAsync();

                var unbalanced = contexts.Single(c => c.GroupId == groupId);
                unbalanced.IsBalanced.Should().BeFalse();
                unbalanced.Net.Should().Be(25m);
            }
        }

        [Fact]
        public async Task ContextBalances_AreIsolatedPerGroup()
        {
            var (_, _, healthyGroup) = await SeedTransferAsync("DKK");
            var (brokenSource, _, brokenGroup) = await SeedTransferAsync("DKK");

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var account = await new AccountRepository(context).GetByIdAsync(brokenSource);

                context.Set<LedgerEntry>().Add(new LedgerEntry(
                    Guid.NewGuid(), brokenSource, account!.OwnerId, brokenGroup, Guid.NewGuid(),
                    LedgerEntryType.Debit, new Money(10m, "DKK"), DateTimeOffset.UtcNow, 98));

                await new UnitOfWork(context).SaveChangesAsync();
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var contexts = await new ReconciliationRepository(context).GetContextBalancesAsync();

                contexts.Single(c => c.GroupId == healthyGroup).IsBalanced.Should().BeTrue();
                contexts.Single(c => c.GroupId == brokenGroup).IsBalanced.Should().BeFalse();
            }
        }
    }
}
