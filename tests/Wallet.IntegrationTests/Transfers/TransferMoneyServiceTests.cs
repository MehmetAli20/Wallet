using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Transfers.TransferMoney;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Transfers;
using Wallet.Domain.Users;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.AccountRepository;
using Wallet.Infrastructure.Persistence.Repositories.UserRepository;

namespace Wallet.IntegrationTests.Transfers
{
    public class TransferMoneyServiceTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;

        public TransferMoneyServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        private TransferMoneyCommandHandler CreateHandler(WalletDbContext context, Guid senderId) =>
            new(new AccountRepository(context),
                new UnitOfWork(context),
                new TransferService(),
                new UserRepository(context),
                TestCurrentUser.For(senderId));

        private async Task SeedUserAsync(Guid userId)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            await new UserRepository(context).AddAsync(
                new User(userId, $"u{userId:N}", $"{userId:N}@test.com", "hash", UserRole.User));
            await new UnitOfWork(context).SaveChangesAsync();
        }

        private async Task<Guid> SeedAccountAsync(Guid ownerId, string currency = "USD")
        {
            var accountId = Guid.NewGuid();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            await new AccountRepository(context).AddAsync(new Account(accountId, ownerId, currency));
            await new UnitOfWork(context).SaveChangesAsync();

            return accountId;
        }

        private static string NewKey() => Guid.NewGuid().ToString();

        [Fact]
        public async Task Transfer_BetweenDifferentUsers_Succeeds()
        {
            var sender = Guid.NewGuid();
            var recipient = Guid.NewGuid();
            await SeedUserAsync(sender);
            await SeedUserAsync(recipient);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(sender)))
            {
                await CreateHandler(context, sender).Handle(
                    new TransferMoneyCommand(recipient, 30m, "USD", NewKey()),
                    CancellationToken.None);
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var repository = new AccountRepository(context);

                var sourceAccount = await repository.GetByOwnerAndCurrencyAsync(sender, "USD");
                var destinationAccount = await repository.GetByOwnerAndCurrencyAsync(recipient, "USD");

                sourceAccount!.Balance.Should().Be(new Money(-30m, "USD"));
                destinationAccount!.Balance.Should().Be(new Money(30m, "USD"));

                sourceAccount.Entries.Should().ContainSingle();
                sourceAccount.Entries[0].Type.Should().Be(LedgerEntryType.Debit);
                destinationAccount.Entries.Should().ContainSingle();
                destinationAccount.Entries[0].Type.Should().Be(LedgerEntryType.Credit);
            }
        }

        [Fact]
        public async Task Transfer_CreatesMissingAccountsImplicitly()
        {
            var sender = Guid.NewGuid();
            var recipient = Guid.NewGuid();
            await SeedUserAsync(sender);
            await SeedUserAsync(recipient);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(sender)))
            {
                await CreateHandler(context, sender).Handle(
                    new TransferMoneyCommand(recipient, 10m, "EUR", NewKey()),
                    CancellationToken.None);
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var repository = new AccountRepository(context);

                (await repository.GetByOwnerAndCurrencyAsync(sender, "EUR")).Should().NotBeNull();
                (await repository.GetByOwnerAndCurrencyAsync(recipient, "EUR")).Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Transfer_NormalizesCurrency()
        {
            var sender = Guid.NewGuid();
            var recipient = Guid.NewGuid();
            await SeedUserAsync(sender);
            await SeedUserAsync(recipient);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(sender)))
            {
                await CreateHandler(context, sender).Handle(
                    new TransferMoneyCommand(recipient, 15m, "usd", NewKey()),
                    CancellationToken.None);
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var accounts = await context.Accounts.IgnoreQueryFilters()
                    .Where(a => a.OwnerId == sender || a.OwnerId == recipient)
                    .ToListAsync();

                accounts.Should().HaveCount(2);
                accounts.Should().OnlyContain(a => a.Currency == "USD");
            }
        }

        [Fact]
        public async Task Transfer_ToUnknownRecipient_Throws()
        {
            var sender = Guid.NewGuid();
            await SeedUserAsync(sender);

            await using var context = _fixture.CreateContext(TestCurrentUser.For(sender));

            var act = async () => await CreateHandler(context, sender).Handle(
                new TransferMoneyCommand(Guid.NewGuid(), 10m, "USD", NewKey()),
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidTransferException>();
        }

        [Fact]
        public async Task FailedTransfer_LeavesDatabaseUnchanged()
        {
            var sender = Guid.NewGuid();
            await SeedUserAsync(sender);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(sender)))
            {
                var act = async () => await CreateHandler(context, sender).Handle(
                    new TransferMoneyCommand(sender, 20m, "USD", NewKey()),
                    CancellationToken.None);

                await act.Should().ThrowAsync<InvalidTransferException>();
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var accounts = await context.Accounts.IgnoreQueryFilters()
                    .Where(a => a.OwnerId == sender)
                    .ToListAsync();

                accounts.Should().BeEmpty();
                (await context.Set<LedgerEntry>().IgnoreQueryFilters().CountAsync(e => e.OwnerId == sender)).Should().Be(0);
            }
        }

        [Fact]
        public async Task Transfer_RollsBackEverything_WhenSaveFails()
        {
            var ownerA = Guid.NewGuid();
            var ownerB = Guid.NewGuid();
            var sourceId = await SeedAccountAsync(ownerA);
            var destinationId = await SeedAccountAsync(ownerB);

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var repository = new AccountRepository(context);
                var source = await repository.GetByIdAsync(sourceId);
                var destination = await repository.GetByIdAsync(destinationId);

                new TransferService().Transfer(source!, destination!, new Money(40m, "USD"));

                context.Set<LedgerEntry>().Add(new LedgerEntry(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    LedgerEntryType.Debit, new Money(1m, "USD"), DateTimeOffset.UtcNow, 99));

                var act = async () => await new UnitOfWork(context).SaveChangesAsync();
                await act.Should().ThrowAsync<DbUpdateException>();
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var repository = new AccountRepository(context);
                var source = await repository.GetByIdAsync(sourceId);
                var destination = await repository.GetByIdAsync(destinationId);

                source!.Balance.Should().Be(new Money(0m, "USD"));
                destination!.Balance.Should().Be(new Money(0m, "USD"));
                source.Entries.Should().BeEmpty();
                destination.Entries.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task ConcurrentTransfers_FromSameAccount_AreRejectedByConcurrencyToken()
        {
            var sourceId = await SeedAccountAsync(Guid.NewGuid());
            var destinationId = await SeedAccountAsync(Guid.NewGuid());

            await using var contextA = _fixture.CreateContext(TestCurrentUser.System);
            await using var contextB = _fixture.CreateContext(TestCurrentUser.System);

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

            await using var verifyContext = _fixture.CreateContext(TestCurrentUser.System);
            var finalSource = await new AccountRepository(verifyContext).GetByIdAsync(sourceId);
            var finalDestination = await new AccountRepository(verifyContext).GetByIdAsync(destinationId);

            var totalAfter = finalSource!.Balance.Amount + finalDestination!.Balance.Amount;

            totalAfter.Should().Be(0m, "an obligations ledger nets to zero; concurrent transfers must not change that");
        }

        [Fact]
        public async Task PessimisticLock_BlocksASecondWriterOnTheSameRow()
        {
            var accountId = await SeedAccountAsync(Guid.NewGuid());

            await using var contextA = _fixture.CreateContext(TestCurrentUser.System);
            await using var contextB = _fixture.CreateContext(TestCurrentUser.System);

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