namespace Wallet.Api.Contracts.Transfers
{
    public record TransferRequest(Guid RecipientUserId, decimal Amount, string Currency);
}