using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users
{
    public static class RefreshTokenRepositoryExtensions
    {
        public static async Task RevokeFamilyAsync(
            this IRefreshTokenRepository refreshTokens,
            Guid familyId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            foreach (var token in await refreshTokens.GetLiveInFamilyAsync(familyId, now, cancellationToken))
                token.Revoke(now);
        }

        public static async Task RevokeAllForUserAsync(
            this IRefreshTokenRepository refreshTokens,
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            foreach (var token in await refreshTokens.GetLiveForUserAsync(userId, now, cancellationToken))
                token.Revoke(now);
        }
    }
}
