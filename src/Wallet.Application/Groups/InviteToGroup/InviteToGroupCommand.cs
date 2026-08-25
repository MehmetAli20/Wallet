using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Groups.InviteToGroup
{
    public record InviteToGroupCommand(Guid GroupId, Guid UserId) : IRequest;
}