using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Users.Register
{
    public record RegisterCommand(string Username, string Email, string Password) : IRequest<Guid>;
}