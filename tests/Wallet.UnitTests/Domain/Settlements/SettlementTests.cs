using FluentAssertions;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;
using Wallet.Domain.Settlements;

namespace Wallet.UnitTests.Domain.Settlements
{
    public class SettlementTests
    {
        private static (Group group, Guid payer, Guid payee) GroupOfTwo()
        {
            var payer = Guid.NewGuid();
            var payee = Guid.NewGuid();
            var group = Group.CreateNamedGroup(Guid.NewGuid(), "Piknik", "TRY", payer);
            group.Invite(payee, payer);
            group.Accept(payee);

            return (group, payer, payee);
        }

        [Fact]
        public void AnAmountAtTheMaximum_IsRecorded()
        {
            var (group, payer, payee) = GroupOfTwo();

            var settlement = Settlement.Record(Guid.NewGuid(), group, payer, payee, Money.MaxAmount, DateTimeOffset.UtcNow);

            settlement.Amount.Amount.Should().Be(Money.MaxAmount);
        }

        [Fact]
        public void AnAmountAboveTheMaximum_IsRejected()
        {
            var (group, payer, payee) = GroupOfTwo();

            var act = () => Settlement.Record(Guid.NewGuid(), group, payer, payee, Money.MaxAmount + 0.01m, DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidTransferException>();
        }

        [Fact]
        public void ASubCentAmount_IsRejected()
        {
            var (group, payer, payee) = GroupOfTwo();

            var act = () => Settlement.Record(Guid.NewGuid(), group, payer, payee, 10.005m, DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidTransferException>();
        }

        [Fact]
        public void SomeoneWhoLeft_CanBePaid_UpToWhatIsOpen()
        {
            var (group, payer, payee) = GroupOfTwo();
            group.Remove(payee, payer);

            var settlement = Settlement.RecordWithFormerMember(
                Guid.NewGuid(), group, payer, payee, 30m, 30m, DateTimeOffset.UtcNow);

            settlement.Amount.Amount.Should().Be(30m);
        }

        [Fact]
        public void PayingSomeoneWhoLeft_MoreThanIsOpen_IsRejected()
        {
            var (group, payer, payee) = GroupOfTwo();
            group.Remove(payee, payer);

            var act = () => Settlement.RecordWithFormerMember(
                Guid.NewGuid(), group, payer, payee, 30.01m, 30m, DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidTransferException>();
        }

        [Fact]
        public void TwoActiveMembers_CannotSettleAsIfOneHadLeft()
        {
            var (group, payer, payee) = GroupOfTwo();

            var act = () => Settlement.RecordWithFormerMember(
                Guid.NewGuid(), group, payer, payee, 30m, 30m, DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidTransferException>();
        }

        [Fact]
        public void SomeoneOutsideTheGroup_CannotSettleWithSomeoneWhoLeft()
        {
            var (group, payer, payee) = GroupOfTwo();
            group.Remove(payee, payer);

            var act = () => Settlement.RecordWithFormerMember(
                Guid.NewGuid(), group, Guid.NewGuid(), payee, 30m, 30m, DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidTransferException>();
        }
    }
}
