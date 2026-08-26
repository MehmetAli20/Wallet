using FluentAssertions;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;
using Wallet.Domain.Groups;

namespace Wallet.UnitTests.Domain.Expenses
{
    public class ExpenseTests
    {
        private static (Group group, List<Guid> members) NewGroup(int memberCount)
        {
            var creator = Guid.NewGuid();
            var group = Group.CreateNamedGroup(Guid.NewGuid(), "Piknik", "TRY", creator);
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

        [Fact]
        public void Scenario1_FivePeople_EqualSplit()
        {
            var (group, members) = NewGroup(5);

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 250m, "Market",
                DateTimeOffset.UtcNow, members);

            expense.Splits.Should().HaveCount(5);
            expense.Splits.Should().OnlyContain(s => s.Share.Amount == 50m);
            expense.Splits.Sum(s => s.Share.Amount).Should().Be(250m);
        }

        [Fact]
        public void Scenario3_FixedShare_SplitsTheRemainderAmongTheRest()
        {
            var (group, members) = NewGroup(5);
            var generous = members[1];

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 100m, "Market",
                DateTimeOffset.UtcNow, members,
                new Dictionary<Guid, decimal> { [generous] = 60m });

            expense.Splits.Single(s => s.ParticipantId == generous).Share.Amount.Should().Be(60m);
            expense.Splits.Where(s => s.ParticipantId != generous)
                .Should().OnlyContain(s => s.Share.Amount == 10m);
            expense.Splits.Sum(s => s.Share.Amount).Should().Be(100m);
        }

        [Fact]
        public void PayerWhoDidNotAttend_IsFixedAtZero()
        {
            var (group, members) = NewGroup(5);
            var payer = members[0];

            var expense = Expense.Create(Guid.NewGuid(), group, payer, 100m, "Market",
                DateTimeOffset.UtcNow, members,
                new Dictionary<Guid, decimal> { [payer] = 0m });

            expense.Splits.Single(s => s.ParticipantId == payer).Share.Amount.Should().Be(0m);
            expense.Splits.Where(s => s.ParticipantId != payer)
                .Should().OnlyContain(s => s.Share.Amount == 25m);
            expense.Splits.Sum(s => s.Share.Amount).Should().Be(100m);
        }

        [Fact]
        public void Rounding_DistributesLeftoverCents_AndStillSumsExactly()
        {
            var (group, members) = NewGroup(6);

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 100m, "Market",
                DateTimeOffset.UtcNow, members);

            expense.Splits.Sum(s => s.Share.Amount).Should().Be(100m);
            expense.Splits.Count(s => s.Share.Amount == 16.67m).Should().Be(4);
            expense.Splits.Count(s => s.Share.Amount == 16.66m).Should().Be(2);
        }

        [Fact]
        public void Rounding_IsDeterministic()
        {
            var (group, members) = NewGroup(3);

            var first = Expense.Create(Guid.NewGuid(), group, members[0], 10m, "A", DateTimeOffset.UtcNow, members);
            var second = Expense.Create(Guid.NewGuid(), group, members[0], 10m, "B", DateTimeOffset.UtcNow, members);

            foreach (var participant in members)
            {
                first.Splits.Single(s => s.ParticipantId == participant).Share.Amount
                    .Should().Be(second.Splits.Single(s => s.ParticipantId == participant).Share.Amount);
            }
        }

        [Fact]
        public void FixedSharesExceedingTheTotal_Throw()
        {
            var (group, members) = NewGroup(3);

            var act = () => Expense.Create(Guid.NewGuid(), group, members[0], 100m, "Market",
                DateTimeOffset.UtcNow, members,
                new Dictionary<Guid, decimal> { [members[1]] = 120m });

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void AllSharesFixed_ButNotSummingToTotal_Throws()
        {
            var (group, members) = NewGroup(2);

            var act = () => Expense.Create(Guid.NewGuid(), group, members[0], 100m, "Market",
                DateTimeOffset.UtcNow, members,
                new Dictionary<Guid, decimal> { [members[0]] = 40m, [members[1]] = 30m });

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void PayerOutsideTheParticipants_Throws()
        {
            var (group, members) = NewGroup(3);

            var act = () => Expense.Create(Guid.NewGuid(), group, members[0], 100m, "Market",
                DateTimeOffset.UtcNow, new List<Guid> { members[1], members[2] });

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void NonMemberParticipant_Throws()
        {
            var (group, members) = NewGroup(2);
            var outsider = Guid.NewGuid();

            var act = () => Expense.Create(Guid.NewGuid(), group, members[0], 100m, "Market",
                DateTimeOffset.UtcNow, new List<Guid> { members[0], outsider });

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void InvitedButNotAcceptedMember_CannotParticipate()
        {
            var creator = Guid.NewGuid();
            var group = Group.CreateNamedGroup(Guid.NewGuid(), "Piknik", "TRY", creator);
            var invited = Guid.NewGuid();
            group.Invite(invited, creator);

            var act = () => Expense.Create(Guid.NewGuid(), group, creator, 100m, "Market",
                DateTimeOffset.UtcNow, new List<Guid> { creator, invited });

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void Expense_InheritsTheGroupCurrency()
        {
            var (group, members) = NewGroup(2);

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 50m, "Market",
                DateTimeOffset.UtcNow, members);

            expense.Total.Currency.Should().Be("TRY");
            expense.Splits.Should().OnlyContain(s => s.Share.Currency == "TRY");
        }
    }
}
