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
        public void Settle_WithValidInput_MovesThePayerTowardZero()
        {
            var payer = NewAccount();
            var payee = NewAccount();

            _service.Settle(payer, payee, new Money(40m, "USD"), GroupId);

            payer.Balance.Should().Be(new Money(40m, "USD"));
            payee.Balance.Should().Be(new Money(-40m, "USD"));
        }

        [Fact]
        public void Settle_RecordsCreditOnPayerAndDebitOnPayee()
        {
            var payer = NewAccount();
            var payee = NewAccount();

            _service.Settle(payer, payee, new Money(40m, "USD"), GroupId);

            payer.Entries.Should().ContainSingle();
            payer.Entries[0].Type.Should().Be(LedgerEntryType.Credit);
            payer.Entries[0].Amount.Should().Be(new Money(40m, "USD"));

            payee.Entries.Should().ContainSingle();
            payee.Entries[0].Type.Should().Be(LedgerEntryType.Debit);
            payee.Entries[0].Amount.Should().Be(new Money(40m, "USD"));
        }

        [Fact]
        public void Settle_ConservesTotalMoney()
        {
            var payer = NewAccount();
            var payee = NewAccount();
            var totalBefore = payer.Balance.Amount + payee.Balance.Amount;

            _service.Settle(payer, payee, new Money(40m, "USD"), GroupId);

            var totalAfter = payer.Balance.Amount + payee.Balance.Amount;
            totalAfter.Should().Be(totalBefore);
        }

        [Fact]
        public void Settle_WithNullPayer_Throws()
        {
            var act = () => _service.Settle(null!, NewAccount(), new Money(40m, "USD"), GroupId);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Settle_WithNullPayee_Throws()
        {
            var act = () => _service.Settle(NewAccount(), null!, new Money(40m, "USD"), GroupId);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Settle_WithNullAmount_Throws()
        {
            var act = () => _service.Settle(NewAccount(), NewAccount(), null!, GroupId);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Settle_ToTheSameAccount_Throws()
        {
            var account = NewAccount();

            var act = () => _service.Settle(account, account, new Money(40m, "USD"), GroupId);

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-40)]
        public void Settle_WithNonPositiveAmount_Throws(decimal amount)
        {
            var act = () => _service.Settle(NewAccount(), NewAccount(), new Money(amount, "USD"), GroupId);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Settle_BeyondWhatIsOwed_TurnsThePayerIntoACreditor()
        {
            var payer = NewAccount();
            var payee = NewAccount();

            _service.Settle(payer, payee, new Money(150m, "USD"), GroupId);

            payer.Balance.Should().Be(new Money(150m, "USD"));
            payee.Balance.Should().Be(new Money(-150m, "USD"));
            payer.Entries.Should().ContainSingle();
            payee.Entries.Should().ContainSingle();
        }

        [Fact]
        public void Settle_WithMismatchedPayeeCurrency_ThrowsAndLeavesBothAccountsUnchanged()
        {
            var payer = NewAccount("USD");
            var payee = NewAccount("EUR");

            var act = () => _service.Settle(payer, payee, new Money(40m, "USD"), GroupId);

            act.Should().Throw<CurrencyMismatchException>();
            payer.Balance.Should().Be(new Money(0m, "USD"));
            payee.Balance.Should().Be(new Money(0m, "EUR"));
            payer.Entries.Should().BeEmpty();
            payee.Entries.Should().BeEmpty();
        }

        [Fact]
        public void Settle_WithMismatchedAmountCurrency_Throws()
        {
            var payer = NewAccount("USD");
            var payee = NewAccount("USD");

            var act = () => _service.Settle(payer, payee, new Money(40m, "EUR"), GroupId);

            act.Should().Throw<CurrencyMismatchException>();
            payer.Balance.Should().Be(new Money(0m, "USD"));
            payee.Balance.Should().Be(new Money(0m, "USD"));
            payer.Entries.Should().BeEmpty();
            payee.Entries.Should().BeEmpty();
        }
    }
}
