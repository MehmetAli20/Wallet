using MediatR;
using Wallet.Application.Abstractions.Activity;
using Wallet.Application.Abstractions.Groups;
using Wallet.Domain.Activity;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Activity.GetGroupActivity
{
    public class GetGroupActivityQueryHandler
        : IRequestHandler<GetGroupActivityQuery, IReadOnlyList<ActivityEntry>>
    {
        private readonly IGroupRepository _groups;
        private readonly IActivityRepository _activity;

        public GetGroupActivityQueryHandler(IGroupRepository groups, IActivityRepository activity)
        {
            _groups = groups;
            _activity = activity;
        }

        public async Task<IReadOnlyList<ActivityEntry>> Handle(
            GetGroupActivityQuery request, CancellationToken cancellationToken)
        {
            _ = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            return await _activity.GetForGroupAsync(
                request.GroupId, request.After, request.Limit, cancellationToken);
        }
    }
}
