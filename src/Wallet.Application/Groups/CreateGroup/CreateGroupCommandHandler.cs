using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.CreateGroup
{
    public  class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Guid>
    {
        private readonly IGroupRepository _groups;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public CreateGroupCommandHandler(IGroupRepository groups, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _groups = groups;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
        {
            var group = Group.CreateNamedGroup(Guid.NewGuid(), request.Name, request.Currency, _currentUser.UserId);
            await _groups.AddAsync(group);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return group.Id;
        }
    }
}
