namespace Wallet.Api.Contracts.Accounts.Responses
{
    public record AccountListResponse(Guid Id, decimal Balance, string Currency);
}
