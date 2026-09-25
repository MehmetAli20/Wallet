using MediatR;
using Wallet.Application.Abstractions.Activity;
using Wallet.Domain.Activity;

namespace Wallet.Application.Activity.GetUnreadActivity
{
    public class GetUnreadActivityQueryHandler
        : IRequestHandler<GetUnreadActivityQuery, IReadOnlyList<GroupUnreadCount>>
    {
        private readonly IActivityRepository _activity;

        public GetUnreadActivityQueryHandler(IActivityRepository activity)
        {
            _activity = activity;
        }

        public async Task<IReadOnlyList<GroupUnreadCount>> Handle(
            GetUnreadActivityQuery request, CancellationToken cancellationToken) =>
            await _activity.GetUnreadCountsAsync(cancellationToken);
    }
}
