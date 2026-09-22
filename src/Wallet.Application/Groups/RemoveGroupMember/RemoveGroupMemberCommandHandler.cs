using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.RemoveGroupMember
{
    public class RemoveGroupMemberCommandHandler : IRequestHandler<RemoveGroupMemberCommand>
    {
        private readonly IGroupRepository _groups;
        private readonly IGroupBalanceRepository _balances;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public RemoveGroupMemberCommandHandler(
            IGroupRepository groups,
            IGroupBalanceRepository balances,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _groups = groups;
            _balances = balances;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(RemoveGroupMemberCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            var actor = _currentUser.UserId;

            if (request.UserId != actor)
            {
                var acting = group.Members.SingleOrDefault(
                    m => m.UserId == actor && m.Status == GroupMemberStatus.Active);

                if (acting is null || acting.Role != GroupMemberRole.Admin)
                    throw new InvalidGroupOperationException("Only an admin can remove another member.");
            }

            var debts = await _balances.GetDebtsAsync(group.Id, cancellationToken);

            if (debts.Any(d => d.DebtorId == request.UserId || d.CreditorId == request.UserId))
                throw new InvalidGroupOperationException(
                    "This member still has open balances in the group. Settle up first, then remove them.");

            group.Remove(request.UserId, actor);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
