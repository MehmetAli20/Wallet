namespace Wallet.Api.Contracts.Transfers
{
    public record ScheduledTransferResponse(
        Guid Id,
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        string Currency,
        DateTimeOffset ScheduledFor,
        string Status,
        string? FailureReason);
}
