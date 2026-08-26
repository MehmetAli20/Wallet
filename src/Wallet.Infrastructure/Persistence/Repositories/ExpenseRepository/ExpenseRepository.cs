using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Domain.Expenses;

namespace Wallet.Infrastructure.Persistence.Repositories.ExpenseRepository
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly WalletDbContext _context;

        public ExpenseRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Expenses
                .Include(e => e.Splits)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Expense>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            return await _context.Expenses
                .Include(e => e.Splits)
                .Where(e => e.GroupId == groupId)
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Expense expense,  CancellationToken cancellationToken = default)
        {
            await _context.Expenses.AddAsync(expense, cancellationToken);
        }
    }
}
