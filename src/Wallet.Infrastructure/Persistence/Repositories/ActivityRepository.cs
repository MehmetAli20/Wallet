using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Activity;
using Wallet.Application.Abstractions.Settlements;
using Wallet.Domain.Activity;
using Wallet.Domain.Settlements;

namespace Wallet.Infrastructure.Persistence.Repositories
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly WalletDbContext _context;

        public ActivityRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ActivityEntry>> GetForGroupAsync(
            Guid groupId, long? after, int limit, CancellationToken cancellationToken = default)
        {
            var query = _context.ActivityEntries.Where(a => a.GroupId == groupId);

            if (after is not null)
                query = query.Where(a => a.Sequence > after.Value);

            return await query
                .OrderByDescending(a => a.Sequence)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }

    public class SettlementRepository : ISettlementRepository
    {
        private readonly WalletDbContext _context;

        public SettlementRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default) =>
            await _context.Settlements.AddAsync(settlement, cancellationToken);

        public async Task<Settlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            await _context.Settlements.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
