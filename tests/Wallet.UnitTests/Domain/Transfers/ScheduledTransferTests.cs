using FluentAssertions;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Transfers;

namespace Wallet.UnitTests.Domain.Transfers
{
    public class ScheduledTransferTests
    {
        private static readonly DateTimeOffset FutureDate = DateTimeOffset.UtcNow.AddDays(1);

        private static ScheduledTransfer NewPendingTransfer() =>
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Money(100m, "USD"), FutureDate);

        [Fact]
        public void Constructor_WithValidInput_SetsPropertiesAndPendingStatus()
        {
            var id = Guid.NewGuid();
            var sourceId = Guid.NewGuid();
            var destinationId = Guid.NewGuid();
            var amount = new Money(100m, "USD");

            var scheduledTransfer = new ScheduledTransfer(id, sourceId, destinationId, amount, FutureDate);

            scheduledTransfer.Id.Should().Be(id);
            scheduledTransfer.SourceAccountId.Should().Be(sourceId);
            scheduledTransfer.DestinationAccountId.Should().Be(destinationId);
            scheduledTransfer.Amount.Should().Be(amount);
            scheduledTransfer.ScheduledFor.Should().Be(FutureDate);
            scheduledTransfer.Status.Should().Be(ScheduledTransferStatus.Pending);
            scheduledTransfer.FailureReason.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithEmptyId_Throws()
        {
            var act = () => new ScheduledTransfer(
                Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), new Money(100m, "USD"), FutureDate);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptySourceAccountId_Throws()
        {
            var act = () => new ScheduledTransfer(
                Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), new Money(100m, "USD"), FutureDate);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyDestinationAccountId_Throws()
        {
            var act = () => new ScheduledTransfer(
                Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, new Money(100m, "USD"), FutureDate);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithSameSourceAndDestination_ThrowsInvalidTransferException()
        {
            var accountId = Guid.NewGuid();

            var act = () => new ScheduledTransfer(
                Guid.NewGuid(), accountId, accountId, new Money(100m, "USD"), FutureDate);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_WithNullAmount_ThrowsArgumentNullException()
        {
            var act = () => new ScheduledTransfer(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!, FutureDate);

            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Constructor_WithNonPositiveAmount_ThrowsArgumentException(decimal amount)
        {
            var act = () => new ScheduledTransfer(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Money(amount, "USD"), FutureDate);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithPastScheduledDate_ThrowsArgumentException()
        {
            var pastDate = DateTimeOffset.UtcNow.AddMinutes(-1);

            var act = () => new ScheduledTransfer(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Money(100m, "USD"), pastDate);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithCurrentScheduledDate_ThrowsArgumentException()
        {
            var act = () => new ScheduledTransfer(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Money(100m, "USD"), DateTimeOffset.UtcNow);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void MarkExecuted_FromPending_SetsStatusToExecuted()
        {
            var transfer = NewPendingTransfer();

            transfer.MarkExecuted();

            transfer.Status.Should().Be(ScheduledTransferStatus.Executed);
        }

        [Fact]
        public void MarkFailed_FromPending_SetsStatusAndReason()
        {
            var transfer = NewPendingTransfer();

            transfer.MarkFailed("Insufficient funds");

            transfer.Status.Should().Be(ScheduledTransferStatus.Failed);
            transfer.FailureReason.Should().Be("Insufficient funds");
        }

        [Fact]
        public void MarkExecuted_WhenAlreadyExecuted_Throws()
        {
            var transfer = NewPendingTransfer();
            transfer.MarkExecuted();

            var act = () => transfer.MarkExecuted();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MarkFailed_WhenAlreadyExecuted_Throws()
        {
            var transfer = NewPendingTransfer();
            transfer.MarkExecuted();

            var act = () => transfer.MarkFailed("too late");

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MarkExecuted_WhenAlreadyFailed_Throws()
        {
            var transfer = NewPendingTransfer();
            transfer.MarkFailed("some reason");

            var act = () => transfer.MarkExecuted();

            act.Should().Throw<InvalidOperationException>();
        }
    }
}