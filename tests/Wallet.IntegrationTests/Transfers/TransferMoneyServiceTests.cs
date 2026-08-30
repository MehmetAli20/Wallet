using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Transfers.TransferMoney;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;
using Wallet.Domain.Transfers;
using Wallet.Domain.Users;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.Settlements;
using Wallet.Infrastructure.Persistence.Repositories.Groups;
using Wallet.Infrastructure.Persistence.Repositories.Accounts;
using Wallet.Infrastructure.Persistence.Repositories.Users;

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
                new GroupRepository(context),
                new UnitOfWork(context),
                new TransferService(),
                new SettlementRepository(context),
                TestCurrentUser.For(senderId),
                new UserRepository(context));

        private async Task SeedUserAsync(Guid userId)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            await new UserRepository(context).AddAsync(
                new User(userId, $"u{userId:N}", $"{userId:N}@test.com", "hash", UserRole.User));
            await new UnitOfWork(context).SaveChangesAsync();
        }

        private async Task<Guid> SeedGroupAsync(string currency, params Guid[] members)
        {
            foreach (var member in members)
                await SeedUserAsync(member);

            var groupId = Guid.NewGuid();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var group = Group.CreateNamedGroup(groupId, "Piknik", currency, members[0]);

            foreach (var member in members.Skip(1))
            {
                group.Invite(member, members[0]);
                group.Accept(member);
            }

            await new GroupRepository(context).AddAsync(group);
            await new UnitOfWork(context).SaveChangesAsync();

            return groupId;
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
        public async Task Transfer_BetweenTwoGroupMembers_Succeeds()
        {
            var sender = Guid.NewGuid();
            var recipient = Guid.NewGuid();
            var groupId = await SeedGroupAsync("USD", sender, recipient);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(sender)))
            {
                await CreateHandler(context, sender).Handle(
                    new TransferMoneyCommand(groupId, recipient, 30m, NewKey()),
                    CancellationToken.None);
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var repository = new AccountRepository(context);

                var source = await repository.GetByOwnerAndCurrencyAsync(sender, "USD");
                var destination = await repository.GetByOwnerAndCurrencyAsync(recipient, "USD");

                source!.Balance.Should().Be(new Money(30m, "USD"));
                destination!.Balance.Should().Be(new Money(-30m, "USD"));

                source.Entries.Should().ContainSingle();
                source.Entries[0].Type.Should().Be(LedgerEntryType.Credit);
                source.Entries[0].GroupId.Should().Be(groupId);

                destination.Entries.Should().ContainSingle();
                destination.Entries[0].Type.Should().Be(LedgerEntryType.Debit);
                destination.Entries[0].GroupId.Should().Be(groupId);
            }
        }

        [Fact]
        public async Task Transfer_CreatesMissingAccountsImplicitly()
        {
            var sender = Guid.NewGuid();
            var recipient = Guid.NewGuid();
            var groupId = await SeedGroupAsync("EUR", sender, recipient);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(sender)))
            {
                await CreateHandler(context, sender).Handle(
                    new TransferMoneyCommand(groupId, recipient, 10m, NewKey()),
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
        public async Task Transfer_UsesTheGroupCurrency()
        {
            var sender = Guid.NewGuid();
            var recipient = Guid.NewGuid();
            var groupId = await SeedGroupAsync("try", sender, recipient);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(sender)))
            {
                await CreateHandler(context, sender).Handle(
                    new TransferMoneyCommand(groupId, recipient, 15m, NewKey()),
                    CancellationToken.None);
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var accounts = await context.Accounts.IgnoreQueryFilters()
                    .Where(a => a.OwnerId == sender || a.OwnerId == recipient)
                    .ToListAsync();

                accounts.Should().HaveCount(2);
                accounts.Should().OnlyContain(a => a.Currency == "TRY");
            }
        }

        [Fact]
        public async Task Transfer_ToANonMember_Throws()
        {
            var sender = Guid.NewGuid();
            var outsider = Guid.NewGuid();
            var groupId = await SeedGroupAsync("USD", sender);

            await using var context = _fixture.CreateContext(TestCurrentUser.For(sender));

            var act = async () => await CreateHandler(context, sender).Handle(
                new TransferMoneyCommand(groupId, outsider, 10m, NewKey()),
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidTransferException>();
        }

        [Fact]
        public async Task Transfer_InAGroupTheSenderDoesNotBelongTo_Throws()
        {
            var owner = Guid.NewGuid();
            var other = Guid.NewGuid();
            var groupId = await SeedGroupAsync("USD", owner, other);

            var stranger = Guid.NewGuid();
            await SeedUserAsync(stranger);

            await using var context = _fixture.CreateContext(TestCurrentUser.For(stranger));

            var act = async () => await CreateHandler(context, stranger).Handle(
                new TransferMoneyCommand(groupId, owner, 10m, NewKey()),
                CancellationToken.None);

            await act.Should().ThrowAsync<GroupNotFoundException>();
        }

        [Fact]
        public async Task FailedTransfer_LeavesDatabaseUnchanged()
        {
            var sender = Guid.NewGuid();
            var recipient = Guid.NewGuid();
            var groupId = await SeedGroupAsync("USD", sender, recipient);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(sender)))
            {
                var act = async () => await CreateHandler(context, sender).Handle(
                    new TransferMoneyCommand(groupId, sender, 20m, NewKey()),
                    CancellationToken.None);

                await act.Should().ThrowAsync<InvalidTransferException>();
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var accounts = await context.Accounts.IgnoreQueryFilters()
                    .Where(a => a.OwnerId == sender)
                    .ToListAsync();

                accounts.Should().BeEmpty();
                (await context.Set<LedgerEntry>().IgnoreQueryFilters()
                    .CountAsync(e => e.OwnerId == sender)).Should().Be(0);
            }
        }

        [Fact]
        public async Task Transfer_RollsBackEverything_WhenSaveFails()
        {
            var groupId = Guid.NewGuid();
            var sourceId = await SeedAccountAsync(Guid.NewGuid());
            var destinationId = await SeedAccountAsync(Guid.NewGuid());

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var repository = new AccountRepository(context);
                var source = await repository.GetByIdAsync(sourceId);
                var destination = await repository.GetByIdAsync(destinationId);

                new TransferService().Settle(source!, destination!, new Money(40m, "USD"), groupId);

                context.Set<LedgerEntry>().Add(new LedgerEntry(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), groupId, Guid.NewGuid(),
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
            var groupId = Guid.NewGuid();
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

            transferService.Settle(sourceA!, destinationA!, new Money(80m, "USD"), groupId);
            await new UnitOfWork(contextA).SaveChangesAsync();

            transferService.Settle(sourceB!, destinationB!, new Money(80m, "USD"), groupId);

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
