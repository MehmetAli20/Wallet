using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Groups.RevokeInviteLink
{
    public class RevokeInviteLinkCommandHandler : IRequestHandler<RevokeInviteLinkCommand>
    {
        private readonly IGroupRepository _groups;
        private readonly IGroupInviteLinkRepository _links;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public RevokeInviteLinkCommandHandler(
            IGroupRepository groups,
            IGroupInviteLinkRepository links,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _groups = groups;
            _links = links;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(RevokeInviteLinkCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            if (!group.IsActiveMember(_currentUser.UserId))
                throw new GroupNotFoundException(request.GroupId);

            var link = await _links.GetByIdAsync(request.LinkId, cancellationToken);

            if (link is null || link.GroupId != group.Id)
                throw new GroupNotFoundException(request.GroupId);

            link.Revoke(DateTimeOffset.UtcNow);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
