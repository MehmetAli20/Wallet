using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.GetMyGroups
{
    public class GetMyGroupsQueryHandler : IRequestHandler<GetMyGroupsQuery, IReadOnlyList<Group>>
    {
        private readonly IGroupRepository _groups;

        public GetMyGroupsQueryHandler(IGroupRepository groups, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _groups = groups;
        }

        public async Task<IReadOnlyList<Group>> Handle(GetMyGroupsQuery request, CancellationToken cancellationToken)
        {
            var groups = await _groups.GetAllAsync(cancellationToken);
            return groups;
        }
    }
}
