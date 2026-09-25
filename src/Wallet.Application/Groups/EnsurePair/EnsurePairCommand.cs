using MediatR;

namespace Wallet.Application.Groups.EnsurePair
{
    public record EnsurePairCommand(Guid OtherUserId, string Currency) : IRequest<PairResult>;
}
