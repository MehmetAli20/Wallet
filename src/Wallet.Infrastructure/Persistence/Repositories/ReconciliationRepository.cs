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

        public async Task<IReadOnlyList<CurrencyLedgerTotals>> GetLedgerTotalsAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await _context.Set<LedgerEntry>()
                .IgnoreQueryFilters()
                .GroupBy(e => e.Amount.Currency)
                .Select(g => new
                {
                    Currency = g.Key,
                    Credits = g.Where(e => e.Type == LedgerEntryType.Credit).Sum(e => e.Amount.Amount),
                    Debits = g.Where(e => e.Type == LedgerEntryType.Debit).Sum(e => e.Amount.Amount)
                })
                .ToListAsync(cancellationToken);

            return grouped
                .Select(x => new CurrencyLedgerTotals(x.Currency, x.Credits, x.Debits))
                .ToList();
        }

        public async Task<IReadOnlyList<CurrencyNetPosition>> GetNetPositionsAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await _context.Accounts
                .IgnoreQueryFilters()
                .GroupBy(a => a.Currency)
                .Select(g => new
                {
                    Currency = g.Key,
                    Total = g.Sum(a => EF.Property<decimal>(a, "_balanceAmount"))
                })
                .ToListAsync(cancellationToken);

            return grouped
                .Select(x => new CurrencyNetPosition(x.Currency, x.Total))
                .ToList();
        }

        public async Task<IReadOnlyList<AccountDiscrepancy>> GetAccountDiscrepanciesAsync(CancellationToken cancellationToken = default)
        {
            var rows = await _context.Accounts
                .IgnoreQueryFilters()
                .Select(a => new
                {
                    a.Id,
                    a.Currency,
                    Balance = EF.Property<decimal>(a, "_balanceAmount"),
                    LedgerNet = _context.Set<LedgerEntry>()
                        .IgnoreQueryFilters()
                        .Where(e => e.AccountId == a.Id)
                        .Sum(e => (decimal?)(e.Type == LedgerEntryType.Credit ? e.Amount.Amount : -e.Amount.Amount)) ?? 0m
                })
                .Where(x => x.Balance != x.LedgerNet)
                .ToListAsync(cancellationToken);

            return rows
                .Select(x => new AccountDiscrepancy(x.Id, x.Currency, x.Balance, x.LedgerNet))
                .ToList();
        }
    }
}