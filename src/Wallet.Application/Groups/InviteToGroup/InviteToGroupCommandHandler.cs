using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Users;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Groups.InviteToGroup
{
    public class InviteToGroupCommandHandler : IRequestHandler<InviteToGroupCommand>
    {
        private readonly IGroupRepository _groups;
        private readonly ContactPolicy _contacts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;


        public InviteToGroupCommandHandler(IGroupRepository groups, ContactPolicy contacts, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _groups = groups;
            _contacts = contacts;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(InviteToGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId) ?? throw new GroupNotFoundException(request.GroupId);
            
            await _contacts.EnsureCanReachAsync(request.UserId, cancellationToken);

            group.Invite(request.UserId, _currentUser.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
