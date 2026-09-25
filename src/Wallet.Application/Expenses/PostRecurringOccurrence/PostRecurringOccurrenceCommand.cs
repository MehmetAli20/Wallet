using MediatR;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Expenses.PostRecurringOccurrence
{
    public record PostRecurringOccurrenceCommand(
        Guid RecurringExpenseId,
        DateTimeOffset Occurrence,
        string IdempotencyKey) : IRequest<Guid>, IIdempotentRequest;
}
