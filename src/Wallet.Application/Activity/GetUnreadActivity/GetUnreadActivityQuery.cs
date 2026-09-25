using MediatR;
using Wallet.Domain.Activity;

namespace Wallet.Application.Activity.GetUnreadActivity
{
    public record GetUnreadActivityQuery : IRequest<IReadOnlyList<GroupUnreadCount>>;
}
