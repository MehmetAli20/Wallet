using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Transfers;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories;
using Wallet.Infrastructure.Persistence.Repositories.AccountRepository;

namespace Wallet.IntegrationTests.Reconciliation
{
    public class ReconciliationTests : IClassFixture<PostgresFixture>
    {
        private static readonly Guid GroupId = Guid.NewGuid();

        private readonly PostgresFixture _fixture;
        public ReconciliationTests(PostgresFixture fixture) => _fixture = fixture;

        private async Task<(Guid source, Guid destination)> SeedTransferAsync(string currency)
        {
            var sourceId = Guid.NewGuid();
            var destinationId = Guid.NewGuid();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var repository = new AccountRepository(context);

            var source = new Account(sourceId, Guid.NewGuid(), currency);
            var destination = new Account(destinationId, Guid.NewGuid(), currency);

            await repository.AddAsync(source);
            await repository.AddAsync(destination);
            new TransferService().Transfer(source, destination, new Money(40m, currency), GroupId);
            await new UnitOfWork(context).SaveChangesAsync();

            return (sourceId, destinationId);
        }

        [Fact]
        public async Task HealthyLedger_IsBalanced()
        {
            var (source, destination) = await SeedTransferAsync("USD");

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
            var (sourceId, _) = await SeedTransferAsync("GBP");

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
            var (sourceId, _) = await SeedTransferAsync("EUR");

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
    }
}