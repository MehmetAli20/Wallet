using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Groups;
using Wallet.Domain.Common;
using Wallet.Domain.Groups;

namespace Wallet.Infrastructure.Persistence.Repositories.GroupRepository
{
    public class GroupRepository : IGroupRepository
    {
        private readonly WalletDbContext _context;

        public GroupRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        }

        public async Task<Group?> GetPairAsync(Guid userA, Guid userB, string currency, CancellationToken cancellationToken = default)
        {
            var pairKey = Group.BuildPairKey(userA, userB);
            var normalized = Money.NormalizeCurrency(currency);

            return await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.PairKey == pairKey && g.Currency == normalized, cancellationToken);
        }

        public async Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Groups
                .Include(g => g.Members)
                .OrderBy(g => g.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Group>> GetAllForPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
        {
            return await _context.Groups
                .IgnoreQueryFilters()
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.UserId == partyId))
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
        {
            await _context.Groups.AddAsync(group, cancellationToken);
        }

        public async Task<Group?> GetByInvitationIdAsync(Guid invitationId, CancellationToken cancellationToken = default)
        {
            return await _context.Groups
                .IgnoreQueryFilters()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == invitationId), cancellationToken);
        }

        public async Task<IReadOnlyList<PendingInvitation>> GetPendingInvitationsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Groups
                .IgnoreQueryFilters()
                .SelectMany(g => g.Members
                    .Where(m => m.UserId == userId && m.Status == GroupMemberStatus.Invited)
                    .Select(m => new PendingInvitation(
                        m.Id,
                        g.Id,
                        g.Name,
                        g.Currency,
                        m.InvitedAt,
                        m.InvitedBy!.Value,
                        _context.Users.Where(u => u.Id == m.InvitedBy).Select(u => u.Username).FirstOrDefault()!)))
                .ToListAsync(cancellationToken);
        }
    }
}