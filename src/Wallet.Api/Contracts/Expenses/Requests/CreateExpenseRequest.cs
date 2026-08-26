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
}