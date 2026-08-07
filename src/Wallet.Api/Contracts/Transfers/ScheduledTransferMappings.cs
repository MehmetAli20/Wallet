using Wallet.Domain.Transfers;

namespace Wallet.Api.Contracts.Transfers
{
    public static class ScheduledTransferMappings
    {
        public static ScheduledTransferResponse ToResponse(this ScheduledTransfer transfer) =>
            
            new(
                Id: transfer.Id,
                SourceAccountId: transfer.SourceAccountId,
                DestinationAccountId: transfer.DestinationAccountId,
                Amount: transfer.Amount.Amount,
                Currency: transfer.Amount.Currency,
                ScheduledFor: transfer.ScheduledFor,
                Status: transfer.Status.ToString(),
                FailureReason: transfer.FailureReason);
    }
}