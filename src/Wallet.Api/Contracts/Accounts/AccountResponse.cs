namespace Wallet.Api.Contracts.Accounts
{
    public record AccountResponse(Guid Id, decimal Balance, string Currency, IReadOnlyList<LedgerEntryResponse> Entries);
}