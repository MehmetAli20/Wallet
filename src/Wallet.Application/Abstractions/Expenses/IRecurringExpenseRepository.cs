using Wallet.Domain.Expenses;

namespace Wallet.Application.Abstractions.Expenses
{
    public interface IRecurringExpenseRepository
    {
        Task AddAsync(RecurringExpense recurringExpense, CancellationToken cancellationToken = default);
        Task<RecurringExpense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RecurringExpense>> GetMineAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RecurringExpense>> GetDueAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
    }
}
