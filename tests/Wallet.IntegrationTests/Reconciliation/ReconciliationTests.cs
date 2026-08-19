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
        private readonly PostgresFixture _fixture;
        public ReconciliationTests(PostgresFixture fixture) => _fixture = fixture;

        private async Task<(Guid source, Guid destination)> SeedTransferAsync()
        {
            var sourceId = Guid.NewGuid();
            var destinationId = Guid.NewGuid();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var repository = new AccountRepository(context);

            var source = new Account(sourceId, Guid.NewGuid(), "USD");
            var destination = new Account(destinationId, Guid.NewGuid(), "USD");

            await repository.AddAsync(source);
            await repository.AddAsync(destination);
            new TransferService().Transfer(source, destination, new Money(40m, "USD"));
            await new UnitOfWork(context).SaveChangesAsync();

            return (sourceId, destinationId);
        }

        [Fact]
        public async Task HealthyLedger_IsBalanced()
        {
            await SeedTransferAsync();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var repository = new ReconciliationRepository(context);

            (await repository.GetAccountDiscrepanciesAsync()).Should().BeEmpty();
            (await repository.GetLedgerTotalsAsync()).Should().OnlyContain(t => t.IsBalanced);
            (await repository.GetNetPositionsAsync()).Should().OnlyContain(p => p.IsBalanced);
        }

        [Fact]
        public async Task BalanceDriftedFromLedger_IsDetected()
        {
            var (sourceId, _) = await SeedTransferAsync();

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
            var (sourceId, _) = await SeedTransferAsync();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            await context.Database.ExecuteSqlAsync(
                $"UPDATE \"Accounts\" SET \"Balance\" = \"Balance\" + 500 WHERE \"Id\" = {sourceId}");

            var positions = await new ReconciliationRepository(context).GetNetPositionsAsync();

            positions.Should().Contain(p => p.Currency == "USD" && !p.IsBalanced);
        }

        [Fact]
        public async Task Reconciliation_IsGlobal_EvenInAUserContext()
        {
            await SeedTransferAsync();

            await using var context = _fixture.CreateContext(TestCurrentUser.For(Guid.NewGuid()));
            var positions = await new ReconciliationRepository(context).GetNetPositionsAsync();

            positions.Should().NotBeEmpty();
        }
    }
}