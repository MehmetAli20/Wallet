using FluentAssertions;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;
using Wallet.Domain.Groups;

namespace Wallet.UnitTests.Domain.Expenses
{
    public class ExpensePostingServiceTests
    {
        private readonly ExpensePostingService _service = new();

        private static (Group group, List<Guid> members, Dictionary<Guid, Account> accounts) NewGroup(
            int memberCount, string currency = "TRY")
        {
            var creator = Guid.NewGuid();
            var group = Group.CreateNamedGroup(Guid.NewGuid(), "Piknik", currency, creator);
            var members = new List<Guid> { creator };

            for (var i = 1; i < memberCount; i++)
            {
                var user = Guid.NewGuid();
                group.Invite(user, creator);
                group.Accept(user);
                members.Add(user);
            }

            var accounts = members.ToDictionary(
                m => m,
                m => new Account(Guid.NewGuid(), m, currency));

            return (group, members, accounts);
        }

        [Fact]
        public void Scenario1_PayerEndsUpCreditor_EveryoneElseDebtor()
        {
            var (group, members, accounts) = NewGroup(5);
            var payer = members[0];

            var expense = Expense.Create(Guid.NewGuid(), group, payer, 250m, "Market",
                DateTimeOffset.UtcNow, members);

            _service.Post(expense, accounts);

            accounts[payer].Balance.Should().Be(new Money(200m, "TRY"));

            foreach (var other in members.Skip(1))
                accounts[other].Balance.Should().Be(new Money(-50m, "TRY"));

            accounts.Values.Sum(a => a.Balance.Amount).Should().Be(0m);
        }

        [Fact]
        public void Scenario2_TwoExpenses_TwoPayers()
        {
            var (group, members, accounts) = NewGroup(6);
            var marketPayer = members[0];
            var butcherPayer = members[1];

            var market = Expense.Create(Guid.NewGuid(), group, marketPayer, 120m, "Market",
                DateTimeOffset.UtcNow, members);
            var butcher = Expense.Create(Guid.NewGuid(), group, butcherPayer, 90m, "Kasap",
                DateTimeOffset.UtcNow, members);

            _service.Post(market, accounts);
            _service.Post(butcher, accounts);

            accounts[marketPayer].Balance.Should().Be(new Money(85m, "TRY"));
            accounts[butcherPayer].Balance.Should().Be(new Money(55m, "TRY"));

            foreach (var other in members.Skip(2))
                accounts[other].Balance.Should().Be(new Money(-35m, "TRY"));

            accounts.Values.Sum(a => a.Balance.Amount).Should().Be(0m);
        }

        [Fact]
        public void GestureScenario_OneMemberCoversAnother()
        {
            var (group, members, accounts) = NewGroup(5);
            var payer = members[0];
            var generous = members[1];
            var covered = members[4];

            var expense = Expense.Create(Guid.NewGuid(), group, payer, 200m, "Market",
                DateTimeOffset.UtcNow, members,
                new Dictionary<Guid, decimal> { [generous] = 80m, [covered] = 0m });

            _service.Post(expense, accounts);

            accounts[payer].Balance.Should().Be(new Money(160m, "TRY"));
            accounts[generous].Balance.Should().Be(new Money(-80m, "TRY"));
            accounts[members[2]].Balance.Should().Be(new Money(-40m, "TRY"));
            accounts[members[3]].Balance.Should().Be(new Money(-40m, "TRY"));
            accounts[covered].Balance.Should().Be(new Money(0m, "TRY"));

            accounts[covered].Entries.Should().BeEmpty();
            accounts.Values.Sum(a => a.Balance.Amount).Should().Be(0m);
        }

        [Fact]
        public void Posting_KeepsCreditsAndDebitsEqual()
        {
            var (group, members, accounts) = NewGroup(3);
            var payer = members[0];

            var expense = Expense.Create(Guid.NewGuid(), group, payer, 100m, "Market",
                DateTimeOffset.UtcNow, members);

            _service.Post(expense, accounts);

            var entries = accounts.Values.SelectMany(a => a.Entries).ToList();

            var credits = entries.Where(e => e.Type == LedgerEntryType.Credit).Sum(e => e.Amount.Amount);
            var debits = entries.Where(e => e.Type == LedgerEntryType.Debit).Sum(e => e.Amount.Amount);

            credits.Should().Be(debits);

            var payerShare = expense.Splits.Single(s => s.ParticipantId == payer).Share.Amount;
            credits.Should().Be(100m - payerShare);
        }

        [Fact]
        public void Payer_GetsOneCreditPerDebtor_AndNoSelfEntry()
        {
            var (group, members, accounts) = NewGroup(4);
            var payer = members[0];

            var expense = Expense.Create(Guid.NewGuid(), group, payer, 100m, "Market",
                DateTimeOffset.UtcNow, members);

            _service.Post(expense, accounts);

            var entries = accounts[payer].Entries;

            entries.Should().HaveCount(3);
            entries.Should().OnlyContain(e => e.Type == LedgerEntryType.Credit);
            entries.Should().OnlyContain(e => e.Amount == new Money(25m, "TRY"));
            entries.Should().NotContain(e => e.CounterpartyId == payer);
            entries.Select(e => e.CounterpartyId).Should().BeEquivalentTo(members.Skip(1));

            accounts[payer].Balance.Should().Be(new Money(75m, "TRY"));
        }

        [Fact]
        public void EveryDebt_IsWrittenAsTwoMirroredEntries()
        {
            var (group, members, accounts) = NewGroup(3);
            var payer = members[0];
            var debtor = members[1];

            var expense = Expense.Create(Guid.NewGuid(), group, payer, 90m, "Market",
                DateTimeOffset.UtcNow, members);

            _service.Post(expense, accounts);

            var debit = accounts[debtor].Entries.Single();
            debit.Type.Should().Be(LedgerEntryType.Debit);
            debit.CounterpartyId.Should().Be(payer);

            var mirror = accounts[payer].Entries.Single(e => e.CounterpartyId == debtor);
            mirror.Type.Should().Be(LedgerEntryType.Credit);
            mirror.Amount.Should().Be(debit.Amount);
        }

        [Fact]
        public void ZeroShare_ProducesNoLedgerEntry()
        {
            var (group, members, accounts) = NewGroup(5);
            var payer = members[0];
            var absent = members[4];

            var expense = Expense.Create(Guid.NewGuid(), group, payer, 100m, "Market",
                DateTimeOffset.UtcNow, members,
                new Dictionary<Guid, decimal> { [absent] = 0m });

            _service.Post(expense, accounts);

            accounts[absent].Entries.Should().BeEmpty();
            accounts[absent].Balance.Should().Be(new Money(0m, "TRY"));

            accounts.Values.SelectMany(a => a.Entries)
                .Should().NotContain(e => e.CounterpartyId == absent);

            accounts.Values.Sum(a => a.Balance.Amount).Should().Be(0m);
        }

        [Fact]
        public void EveryEntry_CarriesTheGroupId()
        {
            var (group, members, accounts) = NewGroup(3);

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 90m, "Market",
                DateTimeOffset.UtcNow, members);

            _service.Post(expense, accounts);

            accounts.Values.SelectMany(a => a.Entries)
                .Should().OnlyContain(e => e.GroupId == group.Id);
        }

        [Fact]
        public void MissingAccountForAParticipant_Throws()
        {
            var (group, members, accounts) = NewGroup(3);

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 90m, "Market",
                DateTimeOffset.UtcNow, members);

            accounts.Remove(members[2]);

            var act = () => _service.Post(expense, accounts);

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void MissingAccountForThePayer_Throws()
        {
            var (group, members, accounts) = NewGroup(3);

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 90m, "Market",
                DateTimeOffset.UtcNow, members);

            accounts.Remove(members[0]);

            var act = () => _service.Post(expense, accounts);

            act.Should().Throw<InvalidExpenseException>();
        }

        [Fact]
        public void AccountInTheWrongCurrency_Throws()
        {
            var (group, members, accounts) = NewGroup(3);

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 90m, "Market",
                DateTimeOffset.UtcNow, members);

            accounts[members[2]] = new Account(Guid.NewGuid(), members[2], "USD");

            var act = () => _service.Post(expense, accounts);

            act.Should().Throw<CurrencyMismatchException>();
        }

        [Fact]
        public void NothingIsPosted_WhenValidationFails()
        {
            var (group, members, accounts) = NewGroup(3);

            var expense = Expense.Create(Guid.NewGuid(), group, members[0], 90m, "Market",
                DateTimeOffset.UtcNow, members);

            accounts.Remove(members[2]);

            var act = () => _service.Post(expense, accounts);
            act.Should().Throw<InvalidExpenseException>();

            accounts.Values.Should().OnlyContain(a => a.Entries.Count == 0);
        }
    }
}
