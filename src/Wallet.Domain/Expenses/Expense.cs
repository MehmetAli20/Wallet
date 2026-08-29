using Wallet.Domain.Activity;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Domain.Expenses
{
    public class Expense : AggregateRoot
    {
        private readonly List<ExpenseSplit> _splits = new();

        public Guid Id { get; private set; }
        public Guid GroupId { get; private set; }
        public Guid PayerId { get; private set; }
        public Guid CreatedBy { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public Money Total { get; private set; }
        public string Description { get; private set; }
        public DateTimeOffset OccurredAt { get; private set; }
        public IReadOnlyList<ExpenseSplit> Splits => _splits.AsReadOnly();

        public DateTimeOffset? ReversedAt { get; private set; }
        public Guid? ReversedBy { get; private set; }
        public string? ReversalReason { get; private set; }
        public Guid? ReplacesExpenseId { get; private set; }

        public bool IsReversed => ReversedAt is not null;

        private Expense()
        {
            Total = null!;
            Description = null!;
        }

        public static Expense Create(
            Guid id,
            Group group,
            Guid payerId,
            Guid createdBy,
            decimal amount,
            string description,
            DateTimeOffset occurredAt,
            IReadOnlyList<Guid> participants,
            IReadOnlyDictionary<Guid, decimal>? fixedShares = null,
            Guid? replacesExpenseId = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Expense Id cannot be empty.", nameof(id));

            if (group is null)
                throw new ArgumentNullException(nameof(group));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty.", nameof(description));

            if (!group.IsActiveMember(payerId))
                throw new InvalidExpenseException("The payer must be an active group member.");

            if (createdBy == Guid.Empty)
                throw new ArgumentException("CreatedBy cannot be empty.", nameof(createdBy));

            if (!group.IsActiveMember(createdBy))
                throw new InvalidExpenseException("The creator must be an active group member.");

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
                CreatedBy = createdBy,
                CreatedAt = DateTimeOffset.UtcNow,
                Total = new Money(amount, group.Currency),
                Description = description.Trim(),
                OccurredAt = occurredAt.ToUniversalTime(),
                ReplacesExpenseId = replacesExpenseId
            };

            foreach (var (participantId, share) in allocation)
            {
                expense._splits.Add(new ExpenseSplit(
                    Guid.NewGuid(), id, participantId, new Money(share, group.Currency)));
            }

            expense.Raise(new ExpenseCreated(
                expense.Id,
                group.Id,
                createdBy,
                payerId,
                amount,
                group.Currency,
                expense.Description,
                replacesExpenseId is not null,
                expense.CreatedAt));

            return expense;
        }

        public void Reverse(Guid reversedBy, DateTimeOffset at, string? reason = null)
        {
            if (IsReversed)
                throw new InvalidExpenseException("This expense has already been reversed.");

            if (reversedBy == Guid.Empty)
                throw new ArgumentException("ReversedBy cannot be empty.", nameof(reversedBy));

            ReversedAt = at.ToUniversalTime();
            ReversedBy = reversedBy;
            ReversalReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            Raise(new ExpenseReversed(
                Id, GroupId, reversedBy, Total.Amount, Total.Currency,
                Description, ReversalReason, ReversedAt.Value));
        }
    }
}
