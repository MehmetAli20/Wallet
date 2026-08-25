using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Groups.CreateGroup
{
    public record CreateGroupCommand(string Name, string Currency) : IRequest<Guid>;
}
