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

        public async Task<IReadOnlyList<RefreshToken>> GetActiveInFamilyAsync(
            Guid familyId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            await _context.RefreshTokens
                .Where(t => t.FamilyId == familyId
                         && t.UsedAt == null
                         && t.RevokedAt == null
                         && t.ExpiresAt > asOf)
                .ToListAsync(cancellationToken);
    }
}
