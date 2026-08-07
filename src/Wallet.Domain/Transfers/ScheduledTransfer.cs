using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Common;

namespace Wallet.Domain.Transfers
{
    public class ScheduledTransfer
    {
        public Guid Id { get; private set; }
        public Guid SourceAccountId { get; private set; }
        public Guid DestinationAccountId { get; private set; }
        public Money Amount { get; private set; }
        public DateTimeOffset ScheduledFor {  get; private set; }
        public ScheduledTransferStatus Status { get; private set; }
        public string? FailureReason { get; private set; }

        public ScheduledTransfer(Guid id,Guid sourceAccountId, Guid destinationAccountId, Money amount, DateTimeOffset scheduledFor)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));
            if(sourceAccountId == Guid.Empty)
                throw new ArgumentNullException("SourceAccountId cannot be empty.");
            if(destinationAccountId == Guid.Empty)
                throw new ArgumentNullException("DestinationAccountId cannot be empty.");
            if(destinationAccountId == sourceAccountId)
                throw new InvalidOperationException("Destination account and source account must be different.");
            if(amount is null)
                throw new ArgumentNullException("Amount cannot be null.");
            if(amount.Amount <= 0)
                throw new ArgumentException("Amount must be a positive value.", nameof(amount));
            if (scheduledFor <= DateTimeOffset.UtcNow)
                throw new ArgumentException("ScheduledFor must be a future date and time.", nameof(scheduledFor));
            
            Id = id;
            SourceAccountId = sourceAccountId;
            DestinationAccountId = destinationAccountId;
            Amount = amount;
            ScheduledFor = scheduledFor.ToUniversalTime();
            Status = ScheduledTransferStatus.Pending;
        }

        private ScheduledTransfer()
        {
            Amount = null!;
        } 

        public void MarkExecuted()
        {
            EnsurePending();
            Status = ScheduledTransferStatus.Executed;
        }

        public void MarkFailed(string reason)
        {
            EnsurePending();
            Status = ScheduledTransferStatus.Failed;
            FailureReason = reason;
        }

        private void EnsurePending()
        {
            if (Status != ScheduledTransferStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"Cannot transition ScheduledTransfer {Id} from {Status} — only Pending transfers can change state.");
            }
        }
    }
}