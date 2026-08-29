using MediatR;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Expenses.ReviseExpense
{
    public record ReviseExpenseCommand(
        Guid ExpenseId,
        decimal Amount,
        string Description,
        DateTimeOffset OccurredAt,
        IReadOnlyList<Guid> Participants,
        IReadOnlyDictionary<Guid, decimal> FixedShares,
        string IdempotencyKey) : IRequest<Guid>, IIdempotentRequest;
}
