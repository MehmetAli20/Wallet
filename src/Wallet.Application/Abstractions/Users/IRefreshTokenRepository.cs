using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RefreshToken>> GetLiveInFamilyAsync(
            Guid familyId, DateTimeOffset asOf, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RefreshToken>> GetLiveForUserAsync(
            Guid userId, DateTimeOffset asOf, CancellationToken cancellationToken = default);

        Task<bool> IsFamilyRevokedAsync(Guid familyId, CancellationToken cancellationToken = default);
    }
}