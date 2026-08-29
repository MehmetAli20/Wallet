namespace Wallet.Domain.Activity
{
    public sealed record GroupUnreadCount(Guid GroupId, int Unread);

    public class GroupActivityRead
    {
        public Guid UserId { get; private set; }
        public Guid GroupId { get; private set; }
        public long LastSeenSequence { get; private set; }

        private GroupActivityRead() { }

        public GroupActivityRead(Guid userId, Guid groupId, long lastSeenSequence)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User id cannot be empty.", nameof(userId));

            if (groupId == Guid.Empty)
                throw new ArgumentException("Group id cannot be empty.", nameof(groupId));

            if (lastSeenSequence < 0)
                throw new ArgumentException("Sequence cannot be negative.", nameof(lastSeenSequence));

            UserId = userId;
            GroupId = groupId;
            LastSeenSequence = lastSeenSequence;
        }

        public void Advance(long sequence)
        {
            if (sequence < 0)
                throw new ArgumentException("Sequence cannot be negative.", nameof(sequence));

            if (sequence > LastSeenSequence)
                LastSeenSequence = sequence;
        }
    }
}
