using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Users;
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
            string token, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            await _context.PlaceholderClaims
                .FirstOrDefaultAsync(
                    c => c.Token == token && c.UsedAt == null && c.ExpiresAt > asOf,
                    cancellationToken);
    }
}
