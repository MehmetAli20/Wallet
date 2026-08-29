using MediatR;
using Wallet.Domain.Activity;

namespace Wallet.Application.Activity.GetGroupActivity
{
    public record GetGroupActivityQuery(Guid GroupId, long? After, int Limit)
        : IRequest<IReadOnlyList<ActivityEntry>>;
}
