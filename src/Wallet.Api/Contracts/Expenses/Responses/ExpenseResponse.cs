using Wallet.Domain.Expenses;

namespace Wallet.Api.Contracts.Expenses.Responses
{
    public record ExpenseSplitResponse(Guid ParticipantId, decimal Share);

    public record ExpenseResponse(
        Guid Id,
        Guid GroupId,
        Guid PayerId,
        Guid CreatedBy,
        decimal Amount,
        string Currency,
        string Description,
        DateTimeOffset OccurredAt,
        DateTimeOffset CreatedAt,
        decimal? MyShare,
        bool IsReversed,
        DateTimeOffset? ReversedAt,
        Guid? ReversedBy,
        string? ReversalReason,
        Guid? ReplacesExpenseId,
        IReadOnlyList<ExpenseSplitResponse> Splits);

    public static class ExpenseMappings
    {
        public static ExpenseResponse ToResponse(this Expense expense, Guid viewerId) =>
            new(
                expense.Id,
                expense.GroupId,
                expense.PayerId,
                expense.CreatedBy,
                expense.Total.Amount,
                expense.Total.Currency,
                expense.Description,
                expense.OccurredAt,
                expense.CreatedAt,
                expense.Splits.FirstOrDefault(s => s.ParticipantId == viewerId)?.Share.Amount,
                expense.IsReversed,
                expense.ReversedAt,
                expense.ReversedBy,
                expense.ReversalReason,
                expense.ReplacesExpenseId,
                expense.Splits
                    .Select(s => new ExpenseSplitResponse(s.ParticipantId, s.Share.Amount))
                    .ToList());
    }
}
