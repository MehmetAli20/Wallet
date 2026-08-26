using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Abstractions.Expenses
{
    public interface IExpenseRepository
    {
        Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Expense>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
        Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    }
}
