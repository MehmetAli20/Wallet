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
            foreach (var active in await refreshTokens.GetActiveInFamilyAsync(familyId, now, cancellationToken))
                active.Revoke(now);
        }
    }
}
