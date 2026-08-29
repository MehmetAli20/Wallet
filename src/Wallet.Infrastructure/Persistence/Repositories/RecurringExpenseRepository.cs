using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Domain.Expenses;

namespace Wallet.Infrastructure.Persistence.Repositories
{
    public class RecurringExpenseRepository : IRecurringExpenseRepository
    {
        private readonly WalletDbContext _context;

        public RecurringExpenseRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RecurringExpense recurringExpense, CancellationToken cancellationToken = default) =>
            await _context.RecurringExpenses.AddAsync(recurringExpense, cancellationToken);

        public async Task<RecurringExpense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            await _context.RecurringExpenses
                .Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        public async Task<IReadOnlyList<RecurringExpense>> GetMineAsync(CancellationToken cancellationToken = default) =>
            await _context.RecurringExpenses
                .Include(r => r.Participants)
                .Where(r => r.IsActive)
                .OrderBy(r => r.NextOccurrence)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<RecurringExpense>> GetDueAsync(
            DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            await _context.RecurringExpenses
                .Include(r => r.Participants)
                .Where(r => r.IsActive && r.NextOccurrence <= asOf)
                .OrderBy(r => r.NextOccurrence)
                .ToListAsync(cancellationToken);
    }
}
