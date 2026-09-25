using MediatR;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.GetMyRecurringExpenses
{
    public class GetMyRecurringExpensesQueryHandler
        : IRequestHandler<GetMyRecurringExpensesQuery, IReadOnlyList<RecurringExpense>>
    {
        private readonly IRecurringExpenseRepository _recurring;

        public GetMyRecurringExpensesQueryHandler(IRecurringExpenseRepository recurring)
        {
            _recurring = recurring;
        }

        public async Task<IReadOnlyList<RecurringExpense>> Handle(
            GetMyRecurringExpensesQuery request, CancellationToken cancellationToken) =>
            await _recurring.GetMineAsync(cancellationToken);
    }
}
