using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Users.Login
{
    public record LoginCommand(string Username, string Password) : IRequest<string>;
}
