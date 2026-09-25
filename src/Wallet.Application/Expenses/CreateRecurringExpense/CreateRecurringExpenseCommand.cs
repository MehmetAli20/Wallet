using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.CreateRecurringExpense
{
    public record CreateRecurringExpenseCommand(
        Guid GroupId,
        Guid PayerId,
        decimal Amount,
        string Description,
        RecurrenceInterval Interval,
        DateTimeOffset FirstOccurrence,
        IReadOnlyList<Guid> Participants,
        IReadOnlyDictionary<Guid, decimal> FixedShares,
        string IdempotencyKey) : IRequest<Guid>, IIdempotentRequest;
}
