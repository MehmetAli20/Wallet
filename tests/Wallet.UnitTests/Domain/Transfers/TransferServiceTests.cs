using FluentAssertions;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Transfers;

namespace Wallet.UnitTests.Domain.Transfers
{
    public class TransferServiceTests
    {
        private static readonly Guid GroupId = Guid.NewGuid();

        private readonly TransferService _service = new();

        private static Account NewAccount(string currency = "USD") =>
            new(Guid.NewGuid(), Guid.NewGuid(), currency);

        [Fact]
        public void Transfer_WithValidInput_MovesMoneyBetweenAccounts()
        {
            var source = NewAccount();
            var destination = NewAccount();

            _service.Transfer(source, destination, new Money(40m, "USD"), GroupId);

            source.Balance.Should().Be(new Money(-40m, "USD"));
            destination.Balance.Should().Be(new Money(40m, "USD"));
        }

        [Fact]
        public void Transfer_RecordsDebitOnSourceAndCreditOnDestination()
        {
            var source = NewAccount();
            var destination = NewAccount();

            _service.Transfer(source, destination, new Money(40m, "USD"), GroupId);

            source.Entries.Should().ContainSingle();
            source.Entries[0].Type.Should().Be(LedgerEntryType.Debit);
            source.Entries[0].Amount.Should().Be(new Money(40m, "USD"));

            destination.Entries.Should().ContainSingle();
            destination.Entries[0].Type.Should().Be(LedgerEntryType.Credit);
            destination.Entries[0].Amount.Should().Be(new Money(40m, "USD"));
        }

        [Fact]
        public void Transfer_ConservesTotalMoney()
        {
            var source = NewAccount();
            var destination = NewAccount();
            var totalBefore = source.Balance.Amount + destination.Balance.Amount;

            _service.Transfer(source, destination, new Money(40m, "USD"), GroupId);

            var totalAfter = source.Balance.Amount + destination.Balance.Amount;
            totalAfter.Should().Be(totalBefore);
        }

        [Fact]
        public void Transfer_WithNullSource_Throws()
        {
            var act = () => _service.Transfer(null!, NewAccount(), new Money(40m, "USD"), GroupId);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Transfer_WithNullDestination_Throws()
        {
            var act = () => _service.Transfer(NewAccount(), null!, new Money(40m, "USD"), GroupId);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Transfer_WithNullAmount_Throws()
        {
            var act = () => _service.Transfer(NewAccount(), NewAccount(), null!, GroupId);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Transfer_ToSameAccount_Throws()
        {
            var account = NewAccount();

            var act = () => _service.Transfer(account, account, new Money(40m, "USD"), GroupId);

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-40)]
        public void Transfer_WithNonPositiveAmount_Throws(decimal amount)
        {
            var act = () => _service.Transfer(NewAccount(), NewAccount(), new Money(amount, "USD"), GroupId);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Transfer_BeyondSourceBalance_Succeeds_AndSourceGoesNegative()
        {
            var source = NewAccount();
            var destination = NewAccount();

            _service.Transfer(source, destination, new Money(150m, "USD"), GroupId);

            source.Balance.Should().Be(new Money(-150m, "USD"));
            destination.Balance.Should().Be(new Money(150m, "USD"));
            source.Entries.Should().ContainSingle();
            destination.Entries.Should().ContainSingle();
        }

        [Fact]
        public void Transfer_WithMismatchedDestinationCurrency_ThrowsAndLeavesBothAccountsUnchanged()
        {
            var source = NewAccount("USD");
            var destination = NewAccount("EUR");

            var act = () => _service.Transfer(source, destination, new Money(40m, "USD"), GroupId);

            act.Should().Throw<CurrencyMismatchException>();
            source.Balance.Should().Be(new Money(0m, "USD"));
            destination.Balance.Should().Be(new Money(0m, "EUR"));
            source.Entries.Should().BeEmpty();
            destination.Entries.Should().BeEmpty();
        }

        [Fact]
        public void Transfer_WithMismatchedAmountCurrency_Throws()
        {
            var source = NewAccount("USD");
            var destination = NewAccount("USD");

            var act = () => _service.Transfer(source, destination, new Money(40m, "EUR"), GroupId);

            act.Should().Throw<CurrencyMismatchException>();
            source.Balance.Should().Be(new Money(0m, "USD"));
            destination.Balance.Should().Be(new Money(0m, "USD"));
            source.Entries.Should().BeEmpty();
            destination.Entries.Should().BeEmpty();
        }
    }
}
