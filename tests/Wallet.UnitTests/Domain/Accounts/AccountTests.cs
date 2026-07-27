using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;

namespace Wallet.UnitTests.Domain.Accounts
{
    public class AccountTests
    {
        [Fact]
        public void Constructor_WithValidInput_SetsIdAndBalance()
        {
            var id = Guid.NewGuid();
            var account = new Account(id, new Money(100m, "USD"));

            account.Id.Should().Be(id);
            account.Balance.Should().Be(new Money(100m,"USD"));
        }

        [Fact]
        public void Constructor_WithEmptyId_Throws()
        {
            var act = () => new Account(Guid.Empty, new Money(100m, "USD"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullOpeningBalance_Throws()
        {
            var act = () => new Account(Guid.NewGuid(), null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithNegativeOpeningBalance_Throws()
        {
            var act = () => new Account(Guid.NewGuid(), new Money(-100m, "USD"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Deposit_WithValidAmount_UpdatesBalance()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            account.Deposit(new Money(50m, "USD"));

            account.Balance.Should().Be(new Money(150m, "USD"));
        }

        [Fact]
        public void Deposit_WithNullAmount_Throws()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Deposit(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Withdraw_WithValidAmount_UpdatesBalance()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            account.Withdraw(new Money(50m, "USD"));

            account.Balance.Should().Be(new Money(50m, "USD"));
        }

        [Fact]
        public void Withdraw_WithNullAmount_Throws()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Withdraw(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Withdraw_WithInsufficientFunds_Throws()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Withdraw(new Money(150m, "USD"));
            act.Should().Throw<InvalidOperationException>();
            account.Balance.Should().Be(new Money(100m, "USD")); 
        }

        [Fact]
        public void Withdraw_WithMismatchedCurrency_Throws()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Withdraw(new Money(50m, "EUR"));
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Deposit_WithNegativeAmount_Throws()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Deposit(new Money(-50m, "USD"));
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Withdraw_WithNonPositiveAmount_Throws(decimal amount)
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Withdraw(new Money(amount, "USD"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Deposit_WithMismatchedCurrency_Throws()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Deposit(new Money(50m, "EUR"));
            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Deposit_WithNonPositiveAmount_Throws(decimal amount)
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Deposit(new Money(amount, "USD"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void NewAccount_HasNoLedgerEntries()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            account.Entries.Should().BeEmpty();
        }

        [Fact]
        public void Deposit_RecordsCreditLedgerEntry()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            account.Deposit(new Money(50m, "USD"));
            account.Entries.Should().ContainSingle();
            var entry = account.Entries[0];
            entry.Type.Should().Be(LedgerEntryType.Credit);
            entry.Amount.Should().Be(new Money(50m, "USD"));
            entry.AccountId.Should().Be(account.Id);
        }

        [Fact]
        public void Withdraw_RecordsDebitLedgerEntry()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            account.Withdraw(new Money(50m, "USD"));
            account.Entries.Should().ContainSingle();
            var entry = account.Entries[0];
            entry.Type.Should().Be(LedgerEntryType.Debit);
            entry.Amount.Should().Be(new Money(50m, "USD"));
            entry.AccountId.Should().Be(account.Id);
        }

        [Fact]
        public void MultipleOperations_AppendEntriesInOrder()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            account.Deposit(new Money(50m, "USD"));
            account.Withdraw(new Money(30m, "USD"));
            account.Entries.Should().HaveCount(2);
            account.Entries[0].Type.Should().Be(LedgerEntryType.Credit);
            account.Entries[1].Type.Should().Be(LedgerEntryType.Debit);
        }

        [Fact]
        public void FailedWithdraw_DoesNotRecordLedgerEntry()
        {
            var account = new Account(Guid.NewGuid(), new Money(100m, "USD"));
            var act = () => account.Withdraw(new Money(150m, "USD"));

            act.Should().Throw<InvalidOperationException>();
            account.Entries.Should().BeEmpty();
        }
    }
}