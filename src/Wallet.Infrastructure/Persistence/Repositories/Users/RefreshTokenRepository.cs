using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Common;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Repositories.Users
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly WalletDbContext _context;

        public RefreshTokenRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            var hash = SecureToken.Hash(token);

            return await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetLiveInFamilyAsync(
            Guid familyId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            await Live(asOf).Where(t => t.FamilyId == familyId).ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<RefreshToken>> GetLiveForUserAsync(
            Guid userId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            await Live(asOf).Where(t => t.UserId == userId).ToListAsync(cancellationToken);

        public Task<bool> IsFamilyRevokedAsync(Guid familyId, CancellationToken cancellationToken = default) =>
            _context.RefreshTokens.AnyAsync(t => t.FamilyId == familyId && t.RevokedAt != null, cancellationToken);

        private IQueryable<RefreshToken> Live(DateTimeOffset asOf)
        {
            var reuseCutoff = asOf - RefreshToken.ReuseInterval;

            return _context.RefreshTokens.Where(t =>
                t.RevokedAt == null
                && t.ExpiresAt > asOf
                && (t.UsedAt == null || t.UsedAt > reuseCutoff));
        }
    }
}
