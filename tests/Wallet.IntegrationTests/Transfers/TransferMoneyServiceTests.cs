using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Transfers.TransferMoney;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
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

        private static TransferMoneyCommandHandler CreateHandler(WalletDbContext context)
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
                await CreateHandler(context).Handle(
                    new TransferMoneyCommand(sourceId, destinationId, 30m, "USD", Guid.NewGuid().ToString()),
                    CancellationToken.None);
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
                var act = async () => await CreateHandler(context).Handle(
                    new TransferMoneyCommand(sourceId, destinationId, 500m, "USD", Guid.NewGuid().ToString()),
                    CancellationToken.None);
                await act.Should().ThrowAsync<InsufficientFundsException>();
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

                context.Set<LedgerEntry>().Add(new LedgerEntry(Guid.NewGuid(), Guid.NewGuid(), LedgerEntryType.Debit, new Money(1m, "USD"), DateTimeOffset.UtcNow, 99));

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

        [Fact]
        public async Task ConcurrentTransfers_FromSameAccount_AreRejectedByConcurrencyToken()
        {
            var sourceId = Guid.NewGuid();
            var destinationId = Guid.NewGuid();
            await SeedAccountAsync(sourceId, 100m, destinationId, 30m);
            
            await using var contextA = _fixture.CreateContext();
            await using var contextB = _fixture.CreateContext();

            var repositoryA = new AccountRepository(contextA);
            var repositoryB = new AccountRepository(contextB);

            var sourceA = await repositoryA.GetByIdAsync(sourceId);
            var destinationA = await repositoryA.GetByIdAsync(destinationId);
            var sourceB = await repositoryB.GetByIdAsync(sourceId);
            var destinationB = await repositoryB.GetByIdAsync(destinationId);

            var transferService = new TransferService();
            
            transferService.Transfer(sourceA!, destinationA!, new Money(80m, "USD"));
            await new UnitOfWork(contextA).SaveChangesAsync();

            transferService.Transfer(sourceB!, destinationB!, new Money(80m, "USD"));

            var act = async () => await new UnitOfWork(contextB).SaveChangesAsync();
            await act.Should().ThrowAsync<ConcurrencyConflictException>();

            await using var verifyContext = _fixture.CreateContext();
            var finalSource = await new AccountRepository(verifyContext).GetByIdAsync(sourceId);
            var finalDestination = await new AccountRepository(verifyContext).GetByIdAsync(destinationId);

            var totalAfter = finalSource!.Balance.Amount + finalDestination!.Balance.Amount;

            totalAfter.Should().Be(100m + 30m, "the total balance should remain the same after concurrent transfers");
        }

        [Fact]
        public async Task PessimisticLock_BlocksASecondWriterOnTheSameRow()
        {
            var accountId = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            await SeedAccountAsync(accountId, 100m, otherId, 0m);

            await using var contextA = _fixture.CreateContext();
            await using var contextB = _fixture.CreateContext();

            await using var transactionA = await contextA.Database.BeginTransactionAsync();
            await contextA.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM \"Accounts\" WHERE \"Id\" = {accountId} FOR UPDATE");

            await using var transactionB = await contextB.Database.BeginTransactionAsync();

            var act = async () => await contextB.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM \"Accounts\" WHERE \"Id\" = {accountId} FOR UPDATE NOWAIT");

            await act.Should().ThrowAsync<PostgresException>();

            await transactionA.RollbackAsync();
        }
    }
}