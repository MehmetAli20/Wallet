using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.JoinGroupByLink
{
    public class JoinGroupByLinkCommandHandler : IRequestHandler<JoinGroupByLinkCommand, Group>
    {
        private readonly IGroupRepository _groups;
        private readonly IGroupInviteLinkRepository _links;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public JoinGroupByLinkCommandHandler(
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

        public async Task<Group> Handle(JoinGroupByLinkCommand request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var link = await _links.GetUsableAsync(request.Token, now, cancellationToken)
                ?? throw new InvalidGroupOperationException("This invite link is not valid any more.");

            var group = await _groups.GetByInviteTokenAsync(request.Token, now, cancellationToken)
                ?? throw new GroupNotFoundException(link.GroupId);

            if (group.Join(_currentUser.UserId, link.CreatedBy))
                link.Redeem(_currentUser.UserId, now);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return group;
        }
    }
}
