using MediatR;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Expenses.ReverseExpense
{
    public record ReverseExpenseCommand(
        Guid ExpenseId,
        string? Reason,
        string IdempotencyKey) : IRequest, IIdempotentRequest;
}
