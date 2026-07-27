using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Transfers;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Transfers;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories;

namespace Wallet.IntegrationTests.Transfers
{
    public class TransferMoneyServiceTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;

        public TransferMoneyServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        private static TransferMoneyService CreateService(WalletDbContext context)
        {
            return new(new AccountRepository(context), new UnitOfWork(context), new TransferService());
        }

        private async Task SeedAccountAsync(Guid sourceId, decimal sourceBalance, Guid destinationId, decimal destinationBalance)
        {
            await using var context = _fixture.CreateContext();
            var repository = new AccountRepository(context);

            await repository.AddAsync(new Account(sourceId, new Money(sourceBalance, "USD")));
            await repository.AddAsync(new Account(destinationId, new Money(destinationBalance, "USD")));
            
            await new UnitOfWork(context).SaveChangesAsync();
        }

        [Fact]
        public async Task Transfer_PersistsBothSidesOfTheLedger()
        {
            var sourceId = Guid.NewGuid();
            var destinationId = Guid.NewGuid();

            await SeedAccountAsync(sourceId, 100m, destinationId, 30m);

            await using (var context = _fixture.CreateContext())
            {
                await CreateService(context).TransferAsync(sourceId, destinationId, new Money(30m, "USD"));
            }

            await using (var context = _fixture.CreateContext())
            {
                var repository = new AccountRepository(context);
                var source = await repository.GetByIdAsync(sourceId);
                var destination = await repository.GetByIdAsync(destinationId);

                source!.Balance.Should().Be(new Money(70m, "USD"));
                destination!.Balance.Should().Be(new Money(60m, "USD"));

                source.Entries.Should().ContainSingle();
                source.Entries[0].Type.Should().Be(LedgerEntryType.Debit);
                destination.Entries.Should().ContainSingle();
                destination.Entries[0].Type.Should().Be(LedgerEntryType.Credit);
            }
        }

        [Fact]
        public async Task FailedTransfer_LeavesDatabaseUnchanged()
        {
            var sourceId = Guid.NewGuid();
            var destinationId = Guid.NewGuid();
            await SeedAccountAsync(sourceId, 100m, destinationId, 30m);

            await using(var context = _fixture.CreateContext())
            {
                var act = async() => await CreateService(context).TransferAsync(sourceId, destinationId, new Money(500m, "USD"));
                await act.Should().ThrowAsync<InvalidOperationException>();
            }

            await using(var context = _fixture.CreateContext())
            {
                var repository = new AccountRepository(context);
                var source = await repository.GetByIdAsync(sourceId);
                var destination = await repository.GetByIdAsync(destinationId);

                source!.Balance.Should().Be(new Money(100m, "USD"));
                destination!.Balance.Should().Be(new Money(30m, "USD"));
                source.Entries.Should().BeEmpty();
                destination.Entries.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task Transfer_RollsBackEverything_WhenSaveFails()
        {
            var sourceId = Guid.NewGuid();
            var destinationId = Guid.NewGuid();
            await SeedAccountAsync(sourceId, 100m, destinationId, 30m);

            await using(var context = _fixture.CreateContext())
            {
                var repository = new AccountRepository(context);
                var source = await repository.GetByIdAsync(sourceId);
                var destination = await repository.GetByIdAsync(destinationId);
                
                new TransferService().Transfer(source!, destination!, new Money(40m, "USD"));

                context.Set<LedgerEntry>().Add(new LedgerEntry(Guid.NewGuid(), sourceId, LedgerEntryType.Debit, new Money(1m, "USD"), DateTimeOffset.UtcNow, 0));

                var act = async() => await new UnitOfWork(context).SaveChangesAsync();
                await act.Should().ThrowAsync<DbUpdateException>();
            }

            await using(var context = _fixture.CreateContext())
            {
                var repository = new AccountRepository(context);
                var source = await repository.GetByIdAsync(sourceId);
                var destination = await repository.GetByIdAsync(destinationId);

                source!.Balance.Should().Be(new Money(100m, "USD"));
                destination!.Balance.Should().Be(new Money(30m, "USD"));
                source.Entries.Should().BeEmpty();
                destination.Entries.Should().BeEmpty();
            }
        }
    }
}
