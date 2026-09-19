using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Common;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Repositories.Users
{
    public class PlaceholderClaimRepository : IPlaceholderClaimRepository
    {
        private readonly WalletDbContext _context;

        public PlaceholderClaimRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PlaceholderClaim claim, CancellationToken cancellationToken = default) =>
            await _context.PlaceholderClaims.AddAsync(claim, cancellationToken);

        public async Task<PlaceholderClaim?> GetUsableAsync(
            string token, DateTimeOffset asOf, CancellationToken cancellationToken = default)
        {
            var hash = SecureToken.Hash(token);

            return await _context.PlaceholderClaims
                .FirstOrDefaultAsync(
                    c => c.TokenHash == hash
                      && c.UsedAt == null
                      && c.RevokedAt == null
                      && c.ExpiresAt > asOf,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<PlaceholderClaim>> GetOutstandingForPlaceholderAsync(
            Guid placeholderUserId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            await _context.PlaceholderClaims
                .Where(c => c.PlaceholderUserId == placeholderUserId
                         && c.UsedAt == null
                         && c.RevokedAt == null
                         && c.ExpiresAt > asOf)
                .ToListAsync(cancellationToken);
    }
}
