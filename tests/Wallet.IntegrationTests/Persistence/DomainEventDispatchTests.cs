using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Accounts;
using Wallet.Domain.Activity;
using Wallet.Domain.Common;
using Wallet.Domain.Expenses;
using Wallet.Domain.Groups;
using Wallet.Domain.Users;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.Groups;
using Wallet.Infrastructure.Persistence.Repositories.Expenses;
using Wallet.Infrastructure.Persistence.Repositories.Users;

namespace Wallet.IntegrationTests.Persistence
{
    public class DomainEventDispatchTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;

        public DomainEventDispatchTests(PostgresFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task CreatingAnExpense_WritesItsActivityRow()
        {
            var payer = Guid.NewGuid();
            var other = Guid.NewGuid();
            var groupId = await SeedGroupAsync(payer, other);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(payer)))
            {
                await AddExpenseAsync(context, groupId, payer, other, 100m);
                await new UnitOfWork(context).SaveChangesAsync();
            }

            var entries = await ExpenseActivityAsync(groupId);

            var entry = entries.Should().ContainSingle().Subject;
            entry.ActorId.Should().Be(payer);
            entry.Amount.Should().Be(100m);
            entry.Description.Should().Be("Market");
            entry.Sequence.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task WhenTheSaveFails_TheActivityRowDoesNotSurviveEither()
        {
            var payer = Guid.NewGuid();
            var other = Guid.NewGuid();
            var groupId = await SeedGroupAsync(payer, other);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(payer)))
            {
                await AddExpenseAsync(context, groupId, payer, other, 100m);

                context.Set<LedgerEntry>().Add(new LedgerEntry(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), groupId, Guid.NewGuid(),
                    LedgerEntryType.Debit, new Money(1m, "USD"), DateTimeOffset.UtcNow, 99));

                var act = async () => await new UnitOfWork(context).SaveChangesAsync();

                await act.Should().ThrowAsync<DbUpdateException>();
            }

            (await ExpenseActivityAsync(groupId)).Should().BeEmpty();

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(payer)))
            {
                await AddExpenseAsync(context, groupId, payer, other, 100m);
                await new UnitOfWork(context).SaveChangesAsync();
            }

            (await ExpenseActivityAsync(groupId)).Should().ContainSingle();
        }

        [Fact]
        public async Task ASecondSaveChanges_DoesNotWriteTheActivityRowTwice()
        {
            var payer = Guid.NewGuid();
            var other = Guid.NewGuid();
            var groupId = await SeedGroupAsync(payer, other);

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(payer)))
            {
                await AddExpenseAsync(context, groupId, payer, other, 100m);

                var unitOfWork = new UnitOfWork(context);

                await unitOfWork.SaveChangesAsync();
                await unitOfWork.SaveChangesAsync();
            }

            (await ExpenseActivityAsync(groupId)).Should().ContainSingle();
        }

        [Fact]
        public async Task JoiningAndDecliningBothLeaveATrace_EvenThoughTheMemberRowIsGone()
        {
            var creator = Guid.NewGuid();
            var joiner = Guid.NewGuid();
            var decliner = Guid.NewGuid();

            var groupId = await SeedGroupAsync(creator, joiner);

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                await SeedUserAsync(decliner);

                var repository = new GroupRepository(context);
                var group = await repository.GetByIdAsync(groupId);

                group!.Invite(decliner, creator);
                group.DeclineInvitation(decliner);

                await new UnitOfWork(context).SaveChangesAsync();
            }

            await using (var verify = _fixture.CreateContext(TestCurrentUser.System))
            {
                var group = await new GroupRepository(verify).GetByIdAsync(groupId);
                group!.Members.Should().NotContain(m => m.UserId == decliner);

                var types = await verify.ActivityEntries
                    .Where(a => a.GroupId == groupId)
                    .Select(a => a.Type)
                    .ToListAsync();

                types.Should().Contain(ActivityType.MemberJoined);
                types.Should().Contain(ActivityType.InvitationDeclined);
            }
        }

        private async Task<List<ActivityEntry>> ExpenseActivityAsync(Guid groupId)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);

            return await context.ActivityEntries
                .Where(a => a.GroupId == groupId && a.Type == ActivityType.ExpenseCreated)
                .ToListAsync();
        }

        private static async Task AddExpenseAsync(
            WalletDbContext context, Guid groupId, Guid payer, Guid other, decimal amount)
        {
            var group = await new GroupRepository(context).GetByIdAsync(groupId);

            var expense = Expense.Create(
                Guid.NewGuid(), group!, payer, payer, amount, "Market",
                DateTimeOffset.UtcNow, new List<Guid> { payer, other });

            await new ExpenseRepository(context).AddAsync(expense);
        }

        private async Task SeedUserAsync(Guid userId)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);

            await new UserRepository(context).AddAsync(
                new User(userId, $"u{userId:N}", $"{userId:N}@test.com", "hash", UserRole.User, "Test User"));

            await new UnitOfWork(context).SaveChangesAsync();
        }

        private async Task<Guid> SeedGroupAsync(params Guid[] members)
        {
            foreach (var member in members)
                await SeedUserAsync(member);

            var groupId = Guid.NewGuid();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var group = Group.CreateNamedGroup(groupId, "Piknik", "USD", members[0]);

            foreach (var member in members.Skip(1))
            {
                group.Invite(member, members[0]);
                group.Accept(member);
            }

            await new GroupRepository(context).AddAsync(group);
            await new UnitOfWork(context).SaveChangesAsync();

            return groupId;
        }
    }
}
