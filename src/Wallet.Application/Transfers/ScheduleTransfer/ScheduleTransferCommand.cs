using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Domain.Transfers;

namespace Wallet.Application.Transfers.ScheduleTransfer
{
    public record ScheduleTransferCommand(
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        string Currency,
        DateTimeOffset ScheduledFor,
        string IdempotencyKey) : IRequest<Guid>, IIdempotentRequest;
}
