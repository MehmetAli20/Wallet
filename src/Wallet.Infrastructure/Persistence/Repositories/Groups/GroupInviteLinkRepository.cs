using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Groups;
using Wallet.Domain.Common;
using Wallet.Domain.Groups;

namespace Wallet.Infrastructure.Persistence.Repositories.Groups
{
    public class GroupInviteLinkRepository : IGroupInviteLinkRepository
    {
        private readonly WalletDbContext _context;

        public GroupInviteLinkRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(GroupInviteLink groupInviteLink, CancellationToken cancellationToken = default) =>
            await _context.GroupInviteLinks.AddAsync(groupInviteLink, cancellationToken);

        public async Task<GroupInviteLink?> GetUsableAsync(
            string token, DateTimeOffset asOf, CancellationToken cancellationToken = default)
        {
            var hash = SecureToken.Hash(token);

            return await _context.GroupInviteLinks
                .Include(l => l.Redemptions)
                .FirstOrDefaultAsync(
                    l => l.TokenHash == hash
                      && l.RevokedAt == null
                      && l.Redemptions.Count < l.MaxUses
                      && l.ExpiresAt > asOf,
                    cancellationToken);
        }

        public async Task<GroupInviteLink?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken = default) =>
            await _context.GroupInviteLinks
                .Include(l => l.Redemptions)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        public async Task<IReadOnlyList<GroupInviteLink>> GetOutstandingForGroupAsync(
            Guid groupId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            await _context.GroupInviteLinks
                .Include(l => l.Redemptions)
                .Where(l => l.GroupId == groupId
                         && l.RevokedAt == null
                         && l.Redemptions.Count < l.MaxUses
                         && l.ExpiresAt > asOf)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync(cancellationToken);
    }
}
