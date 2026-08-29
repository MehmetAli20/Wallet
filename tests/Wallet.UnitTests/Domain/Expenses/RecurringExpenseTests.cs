using FluentAssertions;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;
using Wallet.Domain.Groups;

namespace Wallet.UnitTests.Domain.Expenses
{
    public class RecurringExpenseTests
    {
        private static (Group group, List<Guid> members) NewGroup(int memberCount)
        {
            var creator = Guid.NewGuid();
            var group = Group.CreateNamedGroup(Guid.NewGuid(), "Ev", "TRY", creator);
            var members = new List<Guid> { creator };

            for (var i = 1; i < memberCount; i++)
            {
                var user = Guid.NewGuid();
                group.Invite(user, creator);
                group.Accept(user);
                members.Add(user);
            }

            return (group, members);
        }

        private static RecurringExpense NewRecurring(
            RecurrenceInterval interval,
            DateTimeOffset first,
            decimal amount = 900m)
        {
            var (group, members) = NewGroup(3);

            return RecurringExpense.Create(
                Guid.NewGuid(), group, members[0], members[0], amount, "Kira",
                interval, first, members);
        }

        [Fact]
        public void Create_KeepsTheIntentAndStartsActive()
        {
            var (group, members) = NewGroup(3);
            var first = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

            var recurring = RecurringExpense.Create(
                Guid.NewGuid(), group, members[0], members[1], 900m, "  Kira  ",
                RecurrenceInterval.Monthly, first, members,
                new Dictionary<Guid, decimal> { [members[2]] = 0m });

            recurring.IsActive.Should().BeTrue();
            recurring.PayerId.Should().Be(members[0]);
            recurring.CreatedBy.Should().Be(members[1]);
            recurring.Description.Should().Be("Kira");
            recurring.Amount.Currency.Should().Be("TRY");
            recurring.NextOccurrence.Should().Be(first);
            recurring.ParticipantIds().Should().BeEquivalentTo(members);
            recurring.FixedShares().Should().ContainKey(members[2]).WhoseValue.Should().Be(0m);
        }

        [Fact]
        public void AnAmountThatCannotBeSplit_IsRejectedWhenItIsSetUp_NotMonthsLater()
        {
            var (group, members) = NewGroup(3);

            var act = () => RecurringExpense.Create(
                Guid.NewGuid(), group, members[0], members[0], 100m, "Kira",
                RecurrenceInterval.Monthly, DateTimeOffset.UtcNow, members,
                new Dictionary<Guid, decimal> { [members[1]] = 200m });

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void ANonMemberParticipant_IsRejected()
        {
            var (group, members) = NewGroup(2);
            var outsider = Guid.NewGuid();

            var act = () => RecurringExpense.Create(
                Guid.NewGuid(), group, members[0], members[0], 100m, "Kira",
                RecurrenceInterval.Monthly, DateTimeOffset.UtcNow,
                new List<Guid> { members[0], outsider });

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void Weekly_AdvancesBySevenDays()
        {
            var recurring = NewRecurring(
                RecurrenceInterval.Weekly, new DateTimeOffset(2026, 9, 3, 0, 0, 0, TimeSpan.Zero));

            recurring.Advance();

            recurring.NextOccurrence.Should().Be(new DateTimeOffset(2026, 9, 10, 0, 0, 0, TimeSpan.Zero));
        }

        [Fact]
        public void MonthlyOnTheThirtyFirst_ClampsToShortMonths_ThenReturnsToTheAnchor()
        {
            var recurring = NewRecurring(
                RecurrenceInterval.Monthly, new DateTimeOffset(2027, 1, 31, 0, 0, 0, TimeSpan.Zero));

            recurring.Advance();
            recurring.NextOccurrence.Day.Should().Be(28);
            recurring.NextOccurrence.Month.Should().Be(2);

            recurring.Advance();
            recurring.NextOccurrence.Day.Should().Be(31);
            recurring.NextOccurrence.Month.Should().Be(3);

            recurring.Advance();
            recurring.NextOccurrence.Day.Should().Be(30);
            recurring.NextOccurrence.Month.Should().Be(4);
        }

        [Fact]
        public void MonthlyAdvance_KeepsTheTimeOfDay()
        {
            var first = new DateTimeOffset(2026, 9, 15, 9, 30, 0, TimeSpan.Zero);
            var recurring = NewRecurring(RecurrenceInterval.Monthly, first);

            recurring.Advance();

            recurring.NextOccurrence.Should().Be(new DateTimeOffset(2026, 10, 15, 9, 30, 0, TimeSpan.Zero));
        }

        [Fact]
        public void Cancel_StopsIt()
        {
            var recurring = NewRecurring(RecurrenceInterval.Monthly, DateTimeOffset.UtcNow);

            recurring.Cancel();

            recurring.IsActive.Should().BeFalse();
        }

        [Fact]
        public void CancellingTwice_Throws()
        {
            var recurring = NewRecurring(RecurrenceInterval.Monthly, DateTimeOffset.UtcNow);
            recurring.Cancel();

            var act = () => recurring.Cancel();

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void ACancelledRecurringExpense_CannotAdvance()
        {
            var recurring = NewRecurring(RecurrenceInterval.Monthly, DateTimeOffset.UtcNow);
            recurring.Cancel();

            var act = () => recurring.Advance();

            act.Should().Throw<InvalidExpenseException>();
        }
    }
}
