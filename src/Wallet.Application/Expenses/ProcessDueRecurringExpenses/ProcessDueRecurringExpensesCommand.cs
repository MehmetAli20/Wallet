using MediatR;

namespace Wallet.Application.Expenses.ProcessDueRecurringExpenses
{
    public record ProcessDueRecurringExpensesCommand(DateTimeOffset AsOf) : IRequest<int>;
}
