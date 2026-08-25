using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Groups.InviteToGroup
{
    public class InviteToGroupCommandHandler : IRequestHandler<InviteToGroupCommand>
    {
        private readonly IGroupRepository _groups;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;


        public InviteToGroupCommandHandler(IGroupRepository groups, IUserRepository users, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _groups = groups;
            _users = users;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(InviteToGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId) ?? throw new GroupNotFoundException(request.GroupId);
            
            if(!await _users.ExistsAsync(request.UserId, cancellationToken))
            {
                throw new InvalidGroupOperationException("User not found.");
            }

            group.Invite(request.UserId, _currentUser.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
