using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.GetMyGroups
{
    public record GetMyGroupsQuery : IRequest<IReadOnlyList<Group>>;
}
