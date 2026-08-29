namespace Wallet.Api.Contracts.Expenses.Requests
{
    public record ExpenseParticipantRequest(Guid UserId, decimal? Share);

    public record CreateExpenseRequest(
        Guid GroupId,
        Guid PayerId,
        decimal Amount,
        string Description,
        DateTimeOffset OccurredAt,
        IReadOnlyList<ExpenseParticipantRequest> Participants);

    public record ReverseExpenseRequest(string? Reason);

    public record ReviseExpenseRequest(
        decimal Amount,
        string Description,
        DateTimeOffset OccurredAt,
        IReadOnlyList<ExpenseParticipantRequest> Participants);
    public record CreateRecurringExpenseRequest(
        Guid GroupId,
        Guid PayerId,
        decimal Amount,
        string Description,
        Wallet.Domain.Expenses.RecurrenceInterval Interval,
        DateTimeOffset FirstOccurrence,
        IReadOnlyList<ExpenseParticipantRequest> Participants);
}