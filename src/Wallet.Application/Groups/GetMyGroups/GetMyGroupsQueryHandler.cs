using MediatR;
using Wallet.Application.Abstractions.Groups;

namespace Wallet.Application.Groups.GetMyGroups
{
    public class GetMyGroupsQueryHandler : IRequestHandler<GetMyGroupsQuery, IReadOnlyList<GroupView>>
    {
        private readonly IGroupRepository _groups;

        public GetMyGroupsQueryHandler(IGroupRepository groups)
        {
            _groups = groups;
        }

        public async Task<IReadOnlyList<GroupView>> Handle(GetMyGroupsQuery request, CancellationToken cancellationToken) =>
            await _groups.GetMyGroupViewsAsync(cancellationToken);
    }
}
