namespace Wallet.Api.Contracts.Transfers
{
    public record TransferRequest(Guid GroupId, Guid RecipientUserId, decimal Amount);
}
