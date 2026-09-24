using System;
using Wallet.Domain.Common;

namespace Wallet.Domain.Users
{
    public sealed record IssuedRefreshToken(RefreshToken RefreshToken, string Token)
    {
        public override string ToString() =>
            $"{nameof(IssuedRefreshToken)} {{ Id = {RefreshToken.Id}, FamilyId = {RefreshToken.FamilyId}, Token = *** }}";
    }

    public class RefreshToken
    {
        public const string TokenPrefix = "wrt";

        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);
        public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromDays(30);
        public static readonly TimeSpan ReuseInterval = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan RequestLifetime = TimeSpan.FromMinutes(15);

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid FamilyId { get; private set; }
        public string TokenHash { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset SessionExpiresAt { get; private set; }
        public DateTimeOffset? UsedAt { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }

        private RefreshToken()
        {
            TokenHash = null!;
        }

        public static IssuedRefreshToken StartSession(Guid id, Guid userId, DateTimeOffset now)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User id cannot be empty.", nameof(userId));

            var utcNow = now.ToUniversalTime();

            return Create(id, userId, Guid.NewGuid(), utcNow, utcNow.Add(AbsoluteLifetime));
        }

        public IssuedRefreshToken Successor(Guid id, DateTimeOffset now)
        {
            var utcNow = now.ToUniversalTime();

            if (RevokedAt is not null)
                throw new InvalidOperationException("A revoked refresh token has no successor.");

            if (utcNow >= SessionExpiresAt)
                throw new InvalidOperationException("The session has reached its absolute lifetime.");

            return Create(id, UserId, FamilyId, utcNow, SessionExpiresAt);
        }

        public bool IsActive(DateTimeOffset asOf) =>
            UsedAt is null
            && RevokedAt is null
            && ExpiresAt > asOf.ToUniversalTime();

        public bool IsWithinReuseInterval(DateTimeOffset asOf) =>
            UsedAt is not null
            && RevokedAt is null
            && asOf.ToUniversalTime() - UsedAt.Value <= ReuseInterval;

        public bool CanAuthenticateRequest(DateTimeOffset asOf)
        {
            var now = asOf.ToUniversalTime();

            if (RevokedAt is not null || ExpiresAt <= now)
                return false;

            return UsedAt is null
                ? now - CreatedAt < RequestLifetime
                : IsWithinReuseInterval(now);
        }

        public void MarkUsed(DateTimeOffset at)
        {
            if (!IsActive(at))
                throw new InvalidOperationException("Only an active refresh token can be used.");

            UsedAt = at.ToUniversalTime();
        }

        public void Revoke(DateTimeOffset at)
        {
            if (RevokedAt is not null)
                return;

            RevokedAt = at.ToUniversalTime();
        }

        private static IssuedRefreshToken Create(
            Guid id, Guid userId, Guid familyId, DateTimeOffset now, DateTimeOffset sessionExpiresAt)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            var token = SecureToken.New(TokenPrefix);
            var slidingExpiry = now.Add(Lifetime);

            var refreshToken = new RefreshToken
            {
                Id = id,
                UserId = userId,
                FamilyId = familyId,
                TokenHash = SecureToken.Hash(token),
                CreatedAt = now,
                ExpiresAt = slidingExpiry < sessionExpiresAt ? slidingExpiry : sessionExpiresAt,
                SessionExpiresAt = sessionExpiresAt
            };

            return new IssuedRefreshToken(refreshToken, token);
        }
    }
}
