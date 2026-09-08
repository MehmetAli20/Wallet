using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Repositories.Users
{
    public class LoginAttemptRepository : ILoginAttemptRepository
    {
        private readonly WalletDbContext _context;

        public LoginAttemptRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<LoginAttempt>> GetMostRecentAsync(
            Guid userId, int take, CancellationToken cancellationToken = default)
        {
            return await _context.LoginAttempts
                .Where(attempt => attempt.UserId == userId)
                .OrderByDescending(attempt => attempt.AttemptedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(LoginAttempt attempt, CancellationToken cancellationToken = default)
        {
            await _context.LoginAttempts.AddAsync(attempt, cancellationToken);
        }

        public Task<int> DeleteOlderThanAsync(
            DateTimeOffset cutoff, CancellationToken cancellationToken = default)
        {
            return _context.LoginAttempts
                .Where(attempt => attempt.AttemptedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
