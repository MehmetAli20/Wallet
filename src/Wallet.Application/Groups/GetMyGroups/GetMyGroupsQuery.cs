using MediatR;

namespace Wallet.Application.Groups.GetMyGroups
{
    public record GetMyGroupsQuery : IRequest<IReadOnlyList<GroupView>>;
}
