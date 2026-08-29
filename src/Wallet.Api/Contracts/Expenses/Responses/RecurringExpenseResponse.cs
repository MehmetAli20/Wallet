using Wallet.Domain.Expenses;

namespace Wallet.Api.Contracts.Expenses.Responses
{
    public record RecurringExpenseParticipantResponse(Guid UserId, decimal? Share);

    public record RecurringExpenseResponse(
        Guid Id,
        Guid GroupId,
        Guid PayerId,
        decimal Amount,
        string Currency,
        string Description,
        string Interval,
        DateTimeOffset NextOccurrence,
        IReadOnlyList<RecurringExpenseParticipantResponse> Participants);

    public static class RecurringExpenseMappings
    {
        public static RecurringExpenseResponse ToResponse(this RecurringExpense recurring) =>
            new(
                recurring.Id,
                recurring.GroupId,
                recurring.PayerId,
                recurring.Amount.Amount,
                recurring.Amount.Currency,
                recurring.Description,
                recurring.Interval.ToString(),
                recurring.NextOccurrence,
                recurring.Participants
                    .Select(p => new RecurringExpenseParticipantResponse(p.UserId, p.FixedShare))
                    .ToList());
    }
}
