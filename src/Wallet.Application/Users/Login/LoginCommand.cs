using MediatR;

namespace Wallet.Application.Users.Login
{
    public record LoginCommand(string Username, string Password, string? ClientIp = null)
        : IRequest<AuthTokens>;
}
