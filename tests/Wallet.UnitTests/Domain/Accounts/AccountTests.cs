using FluentAssertions;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;

namespace Wallet.UnitTests.Domain.Accounts
{
    public class AccountTests
    {
        private static Account NewAccount(string currency = "USD") =>
            new(Guid.NewGuid(), Guid.NewGuid(), currency);

        [Fact]
        public void Constructor_WithValidInput_StartsAtZeroInGivenCurrency()
        {
            var id = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var account = new Account(id, ownerId, "USD");

            account.Id.Should().Be(id);
            account.OwnerId.Should().Be(ownerId);
            account.Currency.Should().Be("USD");
            account.Balance.Should().Be(new Money(5m, "USD"));
        }

        [Fact]
        public void Constructor_NormalizesCurrency()
        {
            var account = new Account(Guid.NewGuid(), Guid.NewGuid(), "usd");

            account.Currency.Should().Be("USD");
            account.Balance.Currency.Should().Be("USD");
        }

        [Fact]
        public void Constructor_WithEmptyId_Throws()
        {
            var act = () => new Account(Guid.Empty, Guid.NewGuid(), "USD");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyOwnerId_Throws()
        {
            var act = () => new Account(Guid.NewGuid(), Guid.Empty, "USD");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithInvalidCurrency_Throws()
        {
            var act = () => new Account(Guid.NewGuid(), Guid.NewGuid(), "US");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Credit_WithValidAmount_UpdatesBalance()
        {
            var account = NewAccount();
            account.Credit(new Money(50m, "USD"));

            account.Balance.Should().Be(new Money(50m, "USD"));
        }

        [Fact]
        public void Credit_WithNullAmount_Throws()
        {
            var account = NewAccount();
            var act = () => account.Credit(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Debit_WithValidAmount_UpdatesBalance()
        {
            var account = NewAccount();
            account.Credit(new Money(100m, "USD"));

            account.Debit(new Money(50m, "USD"));

            account.Balance.Should().Be(new Money(50m, "USD"));
        }

        [Fact]
        public void Debit_WithNullAmount_Throws()
        {
            var account = NewAccount();
            var act = () => account.Debit(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Debit_BeyondBalance_GoesNegative_AndRecordsDebitEntry()
        {
            var account = NewAccount();
            account.Credit(new Money(100m, "USD"));

            account.Debit(new Money(150m, "USD"));

            account.Balance.Should().Be(new Money(-50m, "USD"));
            account.Entries.Should().HaveCount(2);
            account.Entries[1].Type.Should().Be(LedgerEntryType.Debit);
            account.Entries[1].Amount.Should().Be(new Money(150m, "USD"));
        }

        [Fact]
        public void Debit_FromZeroBalance_GoesNegative()
        {
            var account = NewAccount();

            account.Debit(new Money(40m, "USD"));

            account.Balance.Should().Be(new Money(-40m, "USD"));
            account.Entries.Should().ContainSingle();
        }

        [Fact]
        public void Debit_WithMismatchedCurrency_Throws()
        {
            var account = NewAccount();
            var act = () => account.Debit(new Money(50m, "EUR"));
            act.Should().Throw<CurrencyMismatchException>();
        }

        [Fact]
        public void Credit_WithNegativeAmount_Throws()
        {
            var account = NewAccount();
            var act = () => account.Credit(new Money(-50m, "USD"));
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Debit_WithNonPositiveAmount_Throws(decimal amount)
        {
            var account = NewAccount();
            var act = () => account.Debit(new Money(amount, "USD"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Credit_WithMismatchedCurrency_Throws()
        {
            var account = NewAccount();
            var act = () => account.Credit(new Money(50m, "EUR"));
            act.Should().Throw<CurrencyMismatchException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Credit_WithNonPositiveAmount_Throws(decimal amount)
        {
            var account = NewAccount();
            var act = () => account.Credit(new Money(amount, "USD"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void NewAccount_HasNoLedgerEntries()
        {
            NewAccount().Entries.Should().BeEmpty();
        }

        [Fact]
        public void Credit_RecordsCreditLedgerEntry()
        {
            var account = NewAccount();
            account.Credit(new Money(50m, "USD"));

            account.Entries.Should().ContainSingle();
            var entry = account.Entries[0];
            entry.Type.Should().Be(LedgerEntryType.Credit);
            entry.Amount.Should().Be(new Money(50m, "USD"));
            entry.AccountId.Should().Be(account.Id);
        }

        [Fact]
        public void Debit_RecordsDebitLedgerEntry()
        {
            var account = NewAccount();
            account.Debit(new Money(50m, "USD"));

            account.Entries.Should().ContainSingle();
            var entry = account.Entries[0];
            entry.Type.Should().Be(LedgerEntryType.Debit);
            entry.Amount.Should().Be(new Money(50m, "USD"));
            entry.AccountId.Should().Be(account.Id);
        }

        [Fact]
        public void MultipleOperations_AppendEntriesInOrder()
        {
            var account = NewAccount();
            account.Credit(new Money(50m, "USD"));
            account.Debit(new Money(30m, "USD"));

            account.Entries.Should().HaveCount(2);
            account.Entries[0].Type.Should().Be(LedgerEntryType.Credit);
            account.Entries[1].Type.Should().Be(LedgerEntryType.Debit);
        }

        [Fact]
        public void FailedDebit_DoesNotRecordLedgerEntry()
        {
            var account = NewAccount();
            var act = () => account.Debit(new Money(150m, "EUR"));

            act.Should().Throw<CurrencyMismatchException>();
            account.Entries.Should().BeEmpty();
            account.Balance.Should().Be(new Money(0m, "USD"));
        }
    }
}
