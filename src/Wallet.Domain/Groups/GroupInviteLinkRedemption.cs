using System;

namespace Wallet.Domain.Groups
{
    public class GroupInviteLinkRedemption
    {
        public Guid Id { get; private set; }
        public Guid LinkId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTimeOffset RedeemedAt { get; private set; }

        private GroupInviteLinkRedemption()
        {
        }

        internal GroupInviteLinkRedemption(Guid id, Guid linkId, Guid userId, DateTimeOffset redeemedAt)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            if (linkId == Guid.Empty)
                throw new ArgumentException("Link Id cannot be empty.", nameof(linkId));

            if (userId == Guid.Empty)
                throw new ArgumentException("User Id cannot be empty.", nameof(userId));

            Id = id;
            LinkId = linkId;
            UserId = userId;
            RedeemedAt = redeemedAt.ToUniversalTime();
        }
    }
}
