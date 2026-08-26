using FluentAssertions;
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
            var ownerId = Guid.NewGuid();
            var ledgerEntryType = LedgerEntryType.Credit;
            var amount = new Money(100m, "USD");
            var occurredAt = DateTimeOffset.UtcNow;

            var ledgerEntry = new LedgerEntry(id, accountId, ownerId, Guid.NewGuid(), Guid.NewGuid(), ledgerEntryType, amount, occurredAt, 3);

            ledgerEntry.Id.Should().Be(id);
            ledgerEntry.AccountId.Should().Be(accountId);
            ledgerEntry.OwnerId.Should().Be(ownerId);
            ledgerEntry.Type.Should().Be(ledgerEntryType);
            ledgerEntry.Amount.Should().Be(amount);
            ledgerEntry.OccurredAt.Should().Be(occurredAt);
            ledgerEntry.Sequence.Should().Be(3);
        }

        [Fact]
        public void Constructor_WithNullId_ThrowsArgumentException()
        {
            var act = () => new LedgerEntry(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                LedgerEntryType.Credit, new Money(100m, "USD"), DateTimeOffset.UtcNow, 0);

            act.Should().Throw<ArgumentException>().WithMessage("Ledger entry Id cannot be empty.*");
        }

        [Fact]
        public void Constructor_WithNullAccountId_ThrowsArgumentException()
        {
            var act = () => new LedgerEntry(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                LedgerEntryType.Credit, new Money(100m, "USD"), DateTimeOffset.UtcNow, 0);

            act.Should().Throw<ArgumentException>().WithMessage("Account Id cannot be empty.*");
        }

        [Fact]
        public void Constructor_WithNullAmount_ThrowsArgumentNullException()
        {
            var act = () => new LedgerEntry(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                LedgerEntryType.Credit, null!, DateTimeOffset.UtcNow, 0);

            act.Should().Throw<ArgumentNullException>().WithMessage("Amount cannot be null.*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Constructor_WithNonPositiveAmount_ThrowsArgumentException(decimal amount)
        {
            var act = () => new LedgerEntry(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                LedgerEntryType.Credit, new Money(amount, "USD"), DateTimeOffset.UtcNow, 0);

            act.Should().Throw<ArgumentException>().WithMessage("Amount must be positive.*");
        }

        [Fact]
        public void Constructor_WithDefaultOccurredAt_Throws()
        {
            var act = () => new LedgerEntry(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                LedgerEntryType.Credit, new Money(50m, "USD"), default, 0);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNegativeSequence_Throws()
        {
            var act = () => new LedgerEntry(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                LedgerEntryType.Credit, new Money(50m, "USD"), DateTimeOffset.UtcNow, -1);

            act.Should().Throw<ArgumentException>();
        }
    }
}