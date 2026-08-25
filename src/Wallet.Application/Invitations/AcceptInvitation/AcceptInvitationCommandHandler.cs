using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Invitations.AcceptInvitation
{
    public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand>
    {
        private readonly IGroupRepository _groups;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public AcceptInvitationCommandHandler(IGroupRepository groups, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _groups = groups;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByInvitationIdAsync(request.InvitationId, cancellationToken)
                ?? throw new InvitationNotFoundException(request.InvitationId);

            var member = group.Members.Single(m => m.Id == request.InvitationId);

            if (member.UserId != _currentUser.UserId)
                throw new InvitationNotFoundException(request.InvitationId);

            group.Accept(_currentUser.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
