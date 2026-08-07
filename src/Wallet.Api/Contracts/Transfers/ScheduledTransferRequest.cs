namespace Wallet.Api.Contracts.Transfers
{
    public record ScheduledTransferRequest(
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        string Currency,
        DateTimeOffset ScheduledFor);
}