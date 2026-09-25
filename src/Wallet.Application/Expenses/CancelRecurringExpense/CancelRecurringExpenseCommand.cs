using MediatR;

namespace Wallet.Application.Expenses.CancelRecurringExpense
{
    public record CancelRecurringExpenseCommand(Guid RecurringExpenseId) : IRequest;
}
