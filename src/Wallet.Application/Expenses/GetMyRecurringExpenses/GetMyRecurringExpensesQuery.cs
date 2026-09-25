using MediatR;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.GetMyRecurringExpenses
{
    public record GetMyRecurringExpensesQuery : IRequest<IReadOnlyList<RecurringExpense>>;
}
