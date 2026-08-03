using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;
using Wallet.Domain.Accounts;
using Wallet.Domain.Reconciliation;

namespace Wallet.Infrastructure.Persistence.Repositories
{
    public class ReconciliationRepository : IReconciliationRepository
    {
        private readonly WalletDbContext _context;

        public ReconciliationRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CurrencyBalance>> GetCurrencyBalancesAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await _context.Set<LedgerEntry>()
                .GroupBy(e => e.Amount.Currency)
                .Select(g => new
                {
                    Currency = g.Key,
                    Credits = g.Where(e => e.Type == LedgerEntryType.Credit).Sum(e => e.Amount.Amount),
                    Debits = g.Where(e => e.Type == LedgerEntryType.Debit).Sum(e => e.Amount.Amount)
                })
                .ToListAsync(cancellationToken);

            return grouped.Select(x => new CurrencyBalance(x.Currency, x.Credits, x.Debits)).ToList();
        }
    }
}
