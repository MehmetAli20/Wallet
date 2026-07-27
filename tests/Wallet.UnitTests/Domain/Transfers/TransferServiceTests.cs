using FluentAssertions;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Transfers;

namespace Wallet.UnitTests.Domain.Transfers
{
    public class TransferServiceTests
    {
        private readonly TransferService _service = new();

        private static Account NewAccount(decimal balance, string currency = "USD") =>
            new(Guid.NewGuid(), new Money(balance, currency));

        [Fact]
        public void Transfer_WithValidInput_MovesMoneyBetweenAccounts()
        {
            var source = NewAccount(100m);
            var destination = NewAccount(30m);

            _service.Transfer(source, destination, new Money(40m, "USD"));

            source.Balance.Should().Be(new Money(60m, "USD"));
            destination.Balance.Should().Be(new Money(70m, "USD"));
        }

        [Fact]
        public void Transfer_RecordsDebitOnSourceAndCreditOnDestination()
        {
            var source = NewAccount(100m);
            var destination = NewAccount(30m);

            _service.Transfer(source, destination, new Money(40m, "USD"));

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
            var source = NewAccount(100m);
            var destination = NewAccount(30m);
            var totalBefore = source.Balance.Amount + destination.Balance.Amount;

            _service.Transfer(source, destination, new Money(40m, "USD"));

            var totalAfter = source.Balance.Amount + destination.Balance.Amount;
            totalAfter.Should().Be(totalBefore);
        }

        [Fact]
        public void Transfer_WithNullSource_Throws()
        {
            var act = () => _service.Transfer(null!, NewAccount(30m), new Money(40m, "USD"));

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Transfer_WithNullDestination_Throws()
        {
            var act = () => _service.Transfer(NewAccount(100m), null!, new Money(40m, "USD"));

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Transfer_WithNullAmount_Throws()
        {
            var act = () => _service.Transfer(NewAccount(100m), NewAccount(30m), null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Transfer_ToSameAccount_Throws()
        {
            var account = NewAccount(100m);

            var act = () => _service.Transfer(account, account, new Money(40m, "USD"));

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-40)]
        public void Transfer_WithNonPositiveAmount_Throws(decimal amount)
        {
            var act = () => _service.Transfer(NewAccount(100m), NewAccount(30m), new Money(amount, "USD"));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Transfer_WithInsufficientFunds_ThrowsAndLeavesBothAccountsUnchanged()
        {
            var source = NewAccount(100m);
            var destination = NewAccount(30m);

            var act = () => _service.Transfer(source, destination, new Money(150m, "USD"));

            act.Should().Throw<InvalidOperationException>();
            source.Balance.Should().Be(new Money(100m, "USD"));
            destination.Balance.Should().Be(new Money(30m, "USD"));
            source.Entries.Should().BeEmpty();
            destination.Entries.Should().BeEmpty();
        }

        [Fact]
        public void Transfer_WithMismatchedDestinationCurrency_ThrowsAndLeavesBothAccountsUnchanged()
        {
            var source = NewAccount(100m, "USD");
            var destination = NewAccount(30m, "EUR");

            var act = () => _service.Transfer(source, destination, new Money(40m, "USD"));

            act.Should().Throw<InvalidOperationException>();
            source.Balance.Should().Be(new Money(100m, "USD"));
            destination.Balance.Should().Be(new Money(30m, "EUR"));
            source.Entries.Should().BeEmpty();
            destination.Entries.Should().BeEmpty();
        }

        [Fact]
        public void Transfer_WithMismatchedAmountCurrency_Throws()
        {
            var source = NewAccount(100m, "USD");
            var destination = NewAccount(30m, "USD");

            var act = () => _service.Transfer(source, destination, new Money(40m, "EUR"));

            act.Should().Throw<InvalidOperationException>();
            source.Balance.Should().Be(new Money(100m, "USD"));
        }
    }
}