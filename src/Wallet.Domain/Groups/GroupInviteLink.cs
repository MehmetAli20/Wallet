using System;
using System.Collections.Generic;
using System.Linq;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;

namespace Wallet.Domain.Groups
{
    public sealed record IssuedInviteLink(GroupInviteLink Link, string Token);

    public class GroupInviteLink
    {
        public const string TokenPrefix = "wli";
        public const int MaxAllowedUses = 100;

        private readonly List<GroupInviteLinkRedemption> _redemptions = new();

        public Guid Id { get; private set; }
        public Guid GroupId { get; private set; }
        public string TokenHash { get; private set; }
        public Guid CreatedBy { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public int MaxUses { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }

        public IReadOnlyList<GroupInviteLinkRedemption> Redemptions => _redemptions.AsReadOnly();

        public int UseCount => _redemptions.Count;

        private GroupInviteLink()
        {
            TokenHash = null!;
        }

        public static IssuedInviteLink Issue(
            Guid id, Group group, Guid createdBy, DateTimeOffset expiresAt, int maxUses)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            if (group is null)
                throw new ArgumentNullException(nameof(group));

            if (group.Kind == GroupKind.Pair)
                throw new InvalidGroupOperationException("A pair cannot take a third member. Create a named group instead.");

            if (!group.IsActiveMember(createdBy))
                throw new InvalidGroupOperationException("Only an active member can create an invite link.");

            if (maxUses < 1 || maxUses > MaxAllowedUses)
                throw new InvalidGroupOperationException($"An invite link must allow between 1 and {MaxAllowedUses} uses.");

            var now = DateTimeOffset.UtcNow;

            if (expiresAt.ToUniversalTime() <= now)
                throw new InvalidGroupOperationException("An invite link must expire in the future.");

            var token = SecureToken.New(TokenPrefix);

            var link = new GroupInviteLink
            {
                Id = id,
                GroupId = group.Id,
                TokenHash = SecureToken.Hash(token),
                CreatedBy = createdBy,
                CreatedAt = now,
                ExpiresAt = expiresAt.ToUniversalTime(),
                MaxUses = maxUses
            };

            return new IssuedInviteLink(link, token);
        }

        public bool IsUsable(DateTimeOffset asOf) =>
            RevokedAt is null
            && _redemptions.Count < MaxUses
            && ExpiresAt > asOf.ToUniversalTime();

        public bool Redeem(Guid userId, DateTimeOffset at)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User Id cannot be empty.", nameof(userId));

            if (RevokedAt is not null)
                throw new InvalidGroupOperationException("This invite link has been revoked.");

            if (at.ToUniversalTime() > ExpiresAt)
                throw new InvalidGroupOperationException("This invite link has expired.");

            if (_redemptions.Any(r => r.UserId == userId))
                return false;

            if (_redemptions.Count >= MaxUses)
                throw new InvalidGroupOperationException("This invite link has reached its use limit.");

            _redemptions.Add(new GroupInviteLinkRedemption(Guid.NewGuid(), Id, userId, at));

            return true;
        }

        public void Revoke(DateTimeOffset at)
        {
            if (RevokedAt is not null)
                return;

            RevokedAt = at.ToUniversalTime();
        }
    }
}
