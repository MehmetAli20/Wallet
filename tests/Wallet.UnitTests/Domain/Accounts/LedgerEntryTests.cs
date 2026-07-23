using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;

namespace Wallet.UnitTests.Domain.Accounts
{
    public class LedgerEntryTests
    {
        [Fact]
        public void Constructor_WithValidInput_SetsProperties()
        {
            var id = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var ledgerEntryType = LedgerEntryType.Credit;
            var amount = new Money(100m, "USD");

            var ledgerEntry = new LedgerEntry(id, accountId, ledgerEntryType, amount);

            ledgerEntry.Id.Should().Be(id);
            ledgerEntry.AccountId.Should().Be(accountId);
            ledgerEntry.Type.Should().Be(ledgerEntryType);
            ledgerEntry.Amount.Should().Be(amount);
        }

        [Fact]
        public void Constructor_WithNullId_ThrowsArgumentException()
        {
            var id = Guid.Empty;
            var accountId = Guid.NewGuid();
            var ledgerEntryType = LedgerEntryType.Credit;
            var amount = new Money(100m, "USD");
            var act = () => new LedgerEntry(id, accountId, ledgerEntryType, amount);
            act.Should().Throw<ArgumentException>().WithMessage("Ledger entry Id cannot be empty.*");
        }

        [Fact]
        public void Constructor_WithNullAccountId_ThrowsArgumentException()
        {
            var id = Guid.NewGuid();
            var accountId = Guid.Empty;
            var ledgerEntryType = LedgerEntryType.Credit;
            var amount = new Money(100m, "USD");
            var act = () => new LedgerEntry(id, accountId, ledgerEntryType, amount);
            act.Should().Throw<ArgumentException>().WithMessage("Account Id cannot be empty.*");
        }
        
        [Fact]
        public void Constructor_WithNullAmount_ThrowsArgumentNullException()
        {
            var id = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var ledgerEntryType = LedgerEntryType.Credit;
            Money amount = null;
            var act = () => new LedgerEntry(id, accountId, ledgerEntryType, amount);
            act.Should().Throw<ArgumentNullException>().WithMessage("Amount cannot be null.*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Constructor_WithNonPositiveAmount_ThrowsArgumentException(decimal amount)
        {
            var id = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var ledgerEntryType = LedgerEntryType.Credit;
            var moneyAmount = new Money(amount, "USD");
            var act = () => new LedgerEntry(id, accountId, ledgerEntryType, moneyAmount);
            act.Should().Throw<ArgumentException>().WithMessage("Amount must be positive.*");
        }
    }
}