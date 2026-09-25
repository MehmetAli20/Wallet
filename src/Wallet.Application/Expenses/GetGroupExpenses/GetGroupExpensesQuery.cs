using MediatR;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.GetGroupExpenses
{
    public record GetGroupExpensesQuery(Guid GroupId, bool IncludeReversed, int Skip, int Take)
        : IRequest<IReadOnlyList<Expense>>;
}
