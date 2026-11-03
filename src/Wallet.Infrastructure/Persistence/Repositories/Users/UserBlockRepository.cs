using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Users;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Repositories.Users
{
    public class UserBlockRepository : IUserBlockRepository
    {
        private readonly WalletDbContext _context;

        public UserBlockRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasBlockedAsync(
            Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default) =>
            await _context.UserBlocks
                .IgnoreQueryFilters()
                .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, cancellationToken);

        public async Task<UserBlock?> GetAsync(
            Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default) =>
            await _context.UserBlocks
                .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, cancellationToken);

        public async Task<IReadOnlyList<BlockedUser>> GetBlockedByAsync(
            Guid blockerId, CancellationToken cancellationToken = default) =>
            await _context.UserBlocks
                .Where(b => b.BlockerId == blockerId)
                .OrderBy(b => b.BlockedAt)
                .Select(b => new BlockedUser(
                    b.BlockedId,
                    _context.Users.Where(u => u.Id == b.BlockedId).Select(u => u.DisplayName).First()))
                .ToListAsync(cancellationToken);

        public async Task AddAsync(UserBlock block, CancellationToken cancellationToken = default) =>
            await _context.UserBlocks.AddAsync(block, cancellationToken);

        public void Remove(UserBlock block) =>
            _context.UserBlocks.Remove(block);
    }
}
