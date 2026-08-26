using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Expenses.CreateExpense
{
    public record CreateExpenseCommand(
        Guid GroupId,
        Guid PayerId,
        decimal Amount,
        string Description,
        DateTimeOffset OccurredAt,
        IReadOnlyList<Guid> Participants,
        IReadOnlyDictionary<Guid, decimal> FixedShares,
        string IdempotencyKey) : IRequest<Guid>, IIdempotentRequest;
}