namespace Wallet.Api.Contracts.Accounts.Responses
{
    public record LedgerEntryResponse(Guid Id, string Type, decimal Amount, string Currency, int Sequence, DateTimeOffset OccurredAt);
}