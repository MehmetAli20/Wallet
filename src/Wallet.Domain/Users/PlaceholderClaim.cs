using System.Security.Cryptography;

namespace Wallet.Domain.Users
{
    public class PlaceholderClaim
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; }
        public Guid PlaceholderUserId { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset? UsedAt { get; private set; }

        private PlaceholderClaim()
        {
            Token = null!;
        }

        public static PlaceholderClaim Issue(Guid id, Guid placeholderUserId, DateTimeOffset expiresAt)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            if (placeholderUserId == Guid.Empty)
                throw new ArgumentException("Placeholder id cannot be empty.", nameof(placeholderUserId));

            return new PlaceholderClaim
            {
                Id = id,
                Token = NewToken(),
                PlaceholderUserId = placeholderUserId,
                ExpiresAt = expiresAt.ToUniversalTime()
            };
        }

        public void Use(DateTimeOffset at)
        {
            if (UsedAt is not null)
                throw new InvalidOperationException("This claim has already been used.");

            if (at > ExpiresAt)
                throw new InvalidOperationException("This claim has expired.");

            UsedAt = at.ToUniversalTime();
        }

        private static string NewToken() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }
}
