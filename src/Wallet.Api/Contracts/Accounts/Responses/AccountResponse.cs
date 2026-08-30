namespace Wallet.Api.Contracts.Accounts.Responses
{
    public record AccountResponse(Guid Id, decimal Balance, string Currency);
}
