namespace Wallet.Api.Contracts.Accounts
{
    public record LedgerEntryResponse(Guid Id, string Type, decimal Amount, string Currency, int Sequence, DateTimeOffset OccurredAt);
}