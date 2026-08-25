using MediatR;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Groups;

namespace Wallet.Application.Invitations.GetMyInvitations
{
    public class GetMyInvitationsQueryHandler : IRequestHandler<GetMyInvitationsQuery, IReadOnlyList<PendingInvitation>>
    {
        private readonly IGroupRepository _groups;
        private readonly ICurrentUser _currentUser;

        public GetMyInvitationsQueryHandler(IGroupRepository groups, ICurrentUser currentUser)
        {
            _groups = groups;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<PendingInvitation>> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
        {
            return await _groups.GetPendingInvitationsAsync(_currentUser.UserId, cancellationToken);
        }
    }
}
