namespace Wallet.Domain.Users
{
    public class UserBlock
    {
        public Guid BlockerId { get; private set; }
        public Guid BlockedId { get; private set; }
        public DateTimeOffset BlockedAt { get; private set; }

        private UserBlock() { }

        public UserBlock(Guid blockerId, Guid blockedId, DateTimeOffset blockedAt)
        {
            if (blockerId == Guid.Empty)
                throw new ArgumentException("Blocker id cannot be empty.", nameof(blockerId));

            if (blockedId == Guid.Empty)
                throw new ArgumentException("Blocked id cannot be empty.", nameof(blockedId));

            if (blockerId == blockedId)
                throw new ArgumentException("You cannot block yourself.", nameof(blockedId));

            BlockerId = blockerId;
            BlockedId = blockedId;
            BlockedAt = blockedAt.ToUniversalTime();
        }
    }
}
