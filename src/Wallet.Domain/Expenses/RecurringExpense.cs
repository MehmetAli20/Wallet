using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Domain.Expenses
{
    public enum RecurrenceInterval
    {
        Weekly = 1,
        Monthly = 2
    }

    public class RecurringExpenseParticipant
    {
        public Guid Id { get; private set; }
        public Guid RecurringExpenseId { get; private set; }
        public Guid UserId { get; private set; }
        public decimal? FixedShare { get; private set; }

        private RecurringExpenseParticipant() { }

        internal RecurringExpenseParticipant(Guid id, Guid recurringExpenseId, Guid userId, decimal? fixedShare)
        {
            Id = id;
            RecurringExpenseId = recurringExpenseId;
            UserId = userId;
            FixedShare = fixedShare;
        }
    }

    public class RecurringExpense
    {
        private readonly List<RecurringExpenseParticipant> _participants = new();

        public Guid Id { get; private set; }
        public Guid GroupId { get; private set; }
        public Guid PayerId { get; private set; }
        public Guid CreatedBy { get; private set; }
        public Money Amount { get; private set; }
        public string Description { get; private set; }
        public RecurrenceInterval Interval { get; private set; }
        public DateTimeOffset NextOccurrence { get; private set; }
        public int AnchorDay { get; private set; }
        public bool IsActive { get; private set; }

        public IReadOnlyList<RecurringExpenseParticipant> Participants => _participants.AsReadOnly();

        private RecurringExpense()
        {
            Amount = null!;
            Description = null!;
        }

        public static RecurringExpense Create(
            Guid id,
            Group group,
            Guid payerId,
            Guid createdBy,
            decimal amount,
            string description,
            RecurrenceInterval interval,
            DateTimeOffset firstOccurrence,
            IReadOnlyList<Guid> participants,
            IReadOnlyDictionary<Guid, decimal>? fixedShares = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            if (group is null)
                throw new ArgumentNullException(nameof(group));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty.", nameof(description));

            if (amount <= 0)
                throw new InvalidExpenseException("A recurring expense needs a positive amount.");

            if (!group.IsActiveMember(payerId))
                throw new InvalidExpenseException("The payer must be an active group member.");

            if (!group.IsActiveMember(createdBy))
                throw new InvalidExpenseException("The creator must be an active group member.");

            if (!participants.Contains(payerId))
                throw new InvalidExpenseException("The payer must be one of the participants.");

            foreach (var participant in participants)
            {
                if (!group.IsActiveMember(participant))
                    throw new InvalidExpenseException("Every participant must be an active group member.");
            }

            ShareAllocator.Allocate(amount, participants, fixedShares ?? new Dictionary<Guid, decimal>());

            var recurring = new RecurringExpense
            {
                Id = id,
                GroupId = group.Id,
                PayerId = payerId,
                CreatedBy = createdBy,
                Amount = new Money(amount, group.Currency),
                Description = description.Trim(),
                Interval = interval,
                NextOccurrence = firstOccurrence.ToUniversalTime(),
                AnchorDay = firstOccurrence.ToUniversalTime().Day,
                IsActive = true
            };

            foreach (var participant in participants)
            {
                fixedShares ??= new Dictionary<Guid, decimal>();

                recurring._participants.Add(new RecurringExpenseParticipant(
                    Guid.NewGuid(),
                    id,
                    participant,
                    fixedShares.TryGetValue(participant, out var share) ? share : null));
            }

            return recurring;
        }

        public void Advance()
        {
            if (!IsActive)
                throw new InvalidExpenseException("A cancelled recurring expense cannot advance.");

            NextOccurrence = Interval switch
            {
                RecurrenceInterval.Weekly => NextOccurrence.AddDays(7),
                RecurrenceInterval.Monthly => NextMonthlyOccurrence(),
                _ => throw new InvalidExpenseException($"Unknown interval {Interval}.")
            };
        }


        private DateTimeOffset NextMonthlyOccurrence()
        {
            var next = NextOccurrence.AddDays(-(NextOccurrence.Day - 1)).AddMonths(1);
            var day = Math.Min(AnchorDay, DateTime.DaysInMonth(next.Year, next.Month));

            return next.AddDays(day - 1);
        }

        public void Cancel()
        {
            if (!IsActive)
                throw new InvalidExpenseException("This recurring expense is already cancelled.");

            IsActive = false;
        }

        public IReadOnlyList<Guid> ParticipantIds() =>
            _participants.Select(p => p.UserId).ToList();

        public IReadOnlyDictionary<Guid, decimal> FixedShares() =>
            _participants
                .Where(p => p.FixedShare.HasValue)
                .ToDictionary(p => p.UserId, p => p.FixedShare!.Value);
    }
}
