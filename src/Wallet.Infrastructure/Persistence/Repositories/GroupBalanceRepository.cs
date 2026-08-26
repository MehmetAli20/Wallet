using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Groups;
using Wallet.Domain.Accounts;
using Wallet.Domain.Groups;

namespace Wallet.Infrastructure.Persistence.Repositories
{
    public class GroupBalanceRepository : IGroupBalanceRepository
    {
        private readonly WalletDbContext _context;

        public GroupBalanceRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<MemberPosition>> GetPositionsAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var rows = await _context.Set<LedgerEntry>()
                .Where(e => e.GroupId == groupId)
                .GroupBy(e => e.OwnerId)
                .Select(g => new
                {
                    OwnerId = g.Key,
                    Net = g.Sum(e => e.Type == LedgerEntryType.Credit ? e.Amount.Amount : -e.Amount.Amount)
                })
                .ToListAsync(cancellationToken);

            return rows.Select(r => new MemberPosition(r.OwnerId, r.Net)).ToList();
        }

        public async Task<IReadOnlyList<PairwiseDebt>> GetDebtsAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var rows = await _context.Set<LedgerEntry>()
                .Where(e => e.GroupId == groupId)
                .GroupBy(e => new { e.OwnerId, e.CounterpartyId })
                .Select(g => new
                {
                    g.Key.OwnerId,
                    g.Key.CounterpartyId,
                    Net = g.Sum(e => e.Type == LedgerEntryType.Credit ? e.Amount.Amount : -e.Amount.Amount)
                })
                .ToListAsync(cancellationToken);

            return rows
                .Where(r => r.Net < 0)
                .Select(r => new PairwiseDebt(r.OwnerId, r.CounterpartyId, -r.Net))
                .OrderBy(d => d.DebtorId).ThenBy(d => d.CreditorId)
                .ToList();
        }
    }
}
