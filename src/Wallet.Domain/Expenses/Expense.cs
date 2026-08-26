using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Domain.Expenses
{
    public class Expense
    {
        private readonly List<ExpenseSplit> _splits = new();

        public Guid Id { get; private set; }
        public Guid GroupId { get; private set; }
        public Guid PayerId { get; private set; }
        public Money Total { get; private set; }
        public string Description { get; private set; }
        public DateTimeOffset OccurredAt { get; private set; }
        public IReadOnlyList<ExpenseSplit> Splits => _splits.AsReadOnly();

        private Expense()
        {
            Total = null!;
            Description = null!;
        }

        public static Expense Create(
            Guid id,
            Group group,
            Guid payerId,
            decimal amount,
            string description,
            DateTimeOffset occurredAt,
            IReadOnlyList<Guid> participants,
            IReadOnlyDictionary<Guid, decimal>? fixedShares = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Expense Id cannot be empty.", nameof(id));

            if (group is null)
                throw new ArgumentNullException(nameof(group));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty.", nameof(description));

            if (!group.IsActiveMember(payerId))
                throw new InvalidExpenseException("The payer must be an active group member.");

            if (!participants.Contains(payerId))
                throw new InvalidExpenseException("The payer must be one of the participants.");

            foreach (var participant in participants)
            {
                if (!group.IsActiveMember(participant))
                    throw new InvalidExpenseException("Every participant must be an active group member.");
            }

            var allocation = ShareAllocator.Allocate(
                amount, participants, fixedShares ?? new Dictionary<Guid, decimal>());

            var expense = new Expense
            {
                Id = id,
                GroupId = group.Id,
                PayerId = payerId,
                Total = new Money(amount, group.Currency),
                Description = description.Trim(),
                OccurredAt = occurredAt.ToUniversalTime()
            };

            foreach (var (participantId, share) in allocation)
            {
                expense._splits.Add(new ExpenseSplit(
                    Guid.NewGuid(), id, participantId, new Money(share, group.Currency)));
            }

            return expense;
        }
    }
}
