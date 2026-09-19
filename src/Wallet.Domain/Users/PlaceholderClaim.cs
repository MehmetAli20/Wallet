using System;
using Wallet.Domain.Common;

namespace Wallet.Domain.Users
{
    public sealed record IssuedPlaceholderClaim(PlaceholderClaim Claim, string Token);

    public class PlaceholderClaim
    {
        public const string TokenPrefix = "wpc";

        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(3);

        public Guid Id { get; private set; }
        public string TokenHash { get; private set; }
        public Guid PlaceholderUserId { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset? UsedAt { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }

        private PlaceholderClaim()
        {
            TokenHash = null!;
        }

        public static IssuedPlaceholderClaim Issue(Guid id, Guid placeholderUserId, DateTimeOffset expiresAt)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            if (placeholderUserId == Guid.Empty)
                throw new ArgumentException("Placeholder id cannot be empty.", nameof(placeholderUserId));

            var token = SecureToken.New(TokenPrefix);

            var claim = new PlaceholderClaim
            {
                Id = id,
                TokenHash = SecureToken.Hash(token),
                PlaceholderUserId = placeholderUserId,
                ExpiresAt = expiresAt.ToUniversalTime()
            };

            return new IssuedPlaceholderClaim(claim, token);
        }

        public bool IsUsable(DateTimeOffset asOf) =>
            UsedAt is null
            && RevokedAt is null
            && ExpiresAt > asOf.ToUniversalTime();

        public void Use(DateTimeOffset at)
        {
            if (RevokedAt is not null)
                throw new InvalidOperationException("This claim has been revoked.");

            if (UsedAt is not null)
                throw new InvalidOperationException("This claim has already been used.");

            if (at > ExpiresAt)
                throw new InvalidOperationException("This claim has expired.");

            UsedAt = at.ToUniversalTime();
        }

        public void Revoke(DateTimeOffset at)
        {
            if (RevokedAt is not null || UsedAt is not null)
                return;

            RevokedAt = at.ToUniversalTime();
        }
    }
}
