using Wallet.Domain.Accounts;
using Wallet.Domain.Activity;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Domain.Settlements
{
    public class Settlement : AggregateRoot
    {
        public Guid Id { get; private set; }
        public Guid GroupId { get; private set; }
        public Guid PayerId { get; private set; }
        public Guid PayeeId { get; private set; }
        public Money Amount { get; private set; }
        public DateTimeOffset OccurredAt { get; private set; }

        private Settlement()
        {
            Amount = null!;
        }

        public static Settlement Record(
            Guid id,
            Group group,
            Guid payerId,
            Guid payeeId,
            decimal amount,
            DateTimeOffset occurredAt)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Settlement id cannot be empty.", nameof(id));

            if (group is null)
                throw new ArgumentNullException(nameof(group));

            if (payerId == payeeId)
                throw new InvalidTransferException("A settlement needs two different people.");

            if (!group.IsActiveMember(payerId))
                throw new InvalidTransferException("The payer is not an active member of this group.");

            if (!group.IsActiveMember(payeeId))
                throw new InvalidTransferException("The recipient is not an active member of this group.");

            if (amount <= 0)
                throw new InvalidTransferException("Settlement amount must be greater than zero.");

            var settlement = new Settlement
            {
                Id = id,
                GroupId = group.Id,
                PayerId = payerId,
                PayeeId = payeeId,
                Amount = new Money(amount, group.Currency),
                OccurredAt = occurredAt.ToUniversalTime()
            };

            settlement.Raise(new SettlementRecorded(
                id, group.Id, payerId, payeeId, amount, group.Currency, settlement.OccurredAt));

            return settlement;
        }
    }
}
