using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Expenses.Recurring;
using Wallet.Domain.Expenses;
using Wallet.Domain.Groups;
using Wallet.Domain.Users;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.Expenses;
using Wallet.Infrastructure.Persistence.Repositories.Groups;
using Wallet.Infrastructure.Persistence.Repositories.Accounts;
using Wallet.Infrastructure.Persistence.Repositories.Users;

namespace Wallet.IntegrationTests.Expenses
{
    public class RecurringExpensePostingTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;

        public RecurringExpensePostingTests(PostgresFixture fixture) => _fixture = fixture;

        private static PostRecurringOccurrenceCommandHandler CreateHandler(WalletDbContext context) =>
            new(new RecurringExpenseRepository(context),
                new ExpenseRepository(context),
                new GroupRepository(context),
                new AccountRepository(context),
                new UnitOfWork(context),
                new ExpensePostingService());

        [Fact]
        public async Task PostingAnOccurrence_CreatesTheExpense_MovesBalances_AndAdvancesTheSchedule()
        {
            var payer = Guid.NewGuid();
            var other = Guid.NewGuid();
            var groupId = await SeedGroupAsync(payer, other);

            var occurrence = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
            var recurringId = await SeedRecurringAsync(groupId, payer, other, 900m, occurrence);

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                await CreateHandler(context).Handle(
                    new PostRecurringOccurrenceCommand(recurringId, occurrence, Guid.NewGuid().ToString()),
                    CancellationToken.None);
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var expense = await context.Expenses
                    .Include(e => e.Splits)
                    .SingleAsync(e => e.GroupId == groupId);

                expense.PayerId.Should().Be(payer);
                expense.CreatedBy.Should().Be(payer);
                expense.Total.Amount.Should().Be(900m);
                expense.OccurredAt.Should().Be(occurrence);
                expense.Splits.Should().HaveCount(2);

                var accounts = new AccountRepository(context);
                (await accounts.GetByOwnerAndCurrencyAsync(payer, "TRY"))!.Balance.Amount.Should().Be(450m);
                (await accounts.GetByOwnerAndCurrencyAsync(other, "TRY"))!.Balance.Amount.Should().Be(-450m);

                var recurring = await context.RecurringExpenses.SingleAsync(r => r.Id == recurringId);
                recurring.NextOccurrence.Should().Be(occurrence.AddMonths(1));
            }
        }

        [Fact]
        public async Task TheJobPostsEveryDueItem_AndSurvivesOneThatCannot()
        {
            var payer = Guid.NewGuid();
            var other = Guid.NewGuid();
            var healthyGroup = await SeedGroupAsync(payer, other);

            var occurrence = DateTimeOffset.UtcNow.AddDays(-1);
            await SeedRecurringAsync(healthyGroup, payer, other, 100m, occurrence);

            var brokenGroup = await SeedGroupAsync(payer, other);
            var brokenId = await SeedRecurringAsync(brokenGroup, payer, other, 100m, occurrence);

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var group = await new GroupRepository(context).GetByIdAsync(brokenGroup);
                group!.Remove(other, payer);
                await new UnitOfWork(context).SaveChangesAsync();
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var due = await new RecurringExpenseRepository(context)
                    .GetDueAsync(DateTimeOffset.UtcNow);

                due.Should().HaveCountGreaterThanOrEqualTo(2);
            }

            var posted = 0;

            foreach (var groupId in new[] { healthyGroup, brokenGroup })
            {
                await using var context = _fixture.CreateContext(TestCurrentUser.System);

                var recurring = await context.RecurringExpenses
                    .Include(r => r.Participants)
                    .SingleAsync(r => r.GroupId == groupId);

                try
                {
                    await CreateHandler(context).Handle(
                        new PostRecurringOccurrenceCommand(
                            recurring.Id, recurring.NextOccurrence, Guid.NewGuid().ToString()),
                        CancellationToken.None);

                    posted++;
                }
                catch
                {
                }
            }

            posted.Should().Be(1);

            await using (var verify = _fixture.CreateContext(TestCurrentUser.System))
            {
                (await verify.Expenses.CountAsync(e => e.GroupId == healthyGroup)).Should().Be(1);
                (await verify.Expenses.CountAsync(e => e.GroupId == brokenGroup)).Should().Be(0);

                var broken = await verify.RecurringExpenses.SingleAsync(r => r.Id == brokenId);
                broken.NextOccurrence.Should().BeCloseTo(occurrence.ToUniversalTime(), TimeSpan.FromSeconds(1));
            }
        }

        private async Task<Guid> SeedRecurringAsync(
            Guid groupId, Guid payer, Guid other, decimal amount, DateTimeOffset first)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);

            var group = await new GroupRepository(context).GetByIdAsync(groupId);

            var recurring = RecurringExpense.Create(
                Guid.NewGuid(), group!, payer, payer, amount, "Kira",
                RecurrenceInterval.Monthly, first, new List<Guid> { payer, other });

            await new RecurringExpenseRepository(context).AddAsync(recurring);
            await new UnitOfWork(context).SaveChangesAsync();

            return recurring.Id;
        }

        private async Task SeedUserAsync(Guid userId)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);

            if (await context.Users.AnyAsync(u => u.Id == userId))
                return;

            await new UserRepository(context).AddAsync(
                new User(userId, $"u{userId:N}", $"{userId:N}@test.com", "hash", UserRole.User));

            await new UnitOfWork(context).SaveChangesAsync();
        }

        private async Task<Guid> SeedGroupAsync(params Guid[] members)
        {
            foreach (var member in members)
                await SeedUserAsync(member);

            var groupId = Guid.NewGuid();

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var group = Group.CreateNamedGroup(groupId, "Ev", "TRY", members[0]);

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
