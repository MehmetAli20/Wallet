namespace Wallet.Domain.Activity
{
    public class ActivityEntry
    {
        public Guid Id { get; private set; }
        public long Sequence { get; private set; }
        public Guid GroupId { get; private set; }
        public Guid ActorId { get; private set; }
        public ActivityType Type { get; private set; }
        public Guid? SubjectId { get; private set; }
        public decimal? Amount { get; private set; }
        public string? Currency { get; private set; }
        public string? Description { get; private set; }
        public DateTimeOffset OccurredAt { get; private set; }

        private ActivityEntry() { }

        public ActivityEntry(
            Guid id,
            Guid groupId,
            Guid actorId,
            ActivityType type,
            DateTimeOffset occurredAt,
            Guid? subjectId = null,
            decimal? amount = null,
            string? currency = null,
            string? description = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Activity id cannot be empty.", nameof(id));

            if (groupId == Guid.Empty)
                throw new ArgumentException("Group id cannot be empty.", nameof(groupId));

            if (actorId == Guid.Empty)
                throw new ArgumentException("Actor id cannot be empty.", nameof(actorId));

            Id = id;
            GroupId = groupId;
            ActorId = actorId;
            Type = type;
            OccurredAt = occurredAt.ToUniversalTime();
            SubjectId = subjectId;
            Amount = amount;
            Currency = currency;
            Description = description;
        }
    }
}
