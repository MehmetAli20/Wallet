using MediatR;

namespace Wallet.Application.Activity.MarkActivitySeen
{
    public record MarkActivitySeenCommand(Guid GroupId, long Sequence) : IRequest;
}
