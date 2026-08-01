namespace Wallet.Api.Contracts.Transfers
{
    public record TransferRequest(Guid SourceId, Guid DestinationId, decimal Amount, string Currency);
}