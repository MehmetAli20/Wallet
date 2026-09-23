using MediatR;

namespace Wallet.Application.Users.Logout
{
    public record LogoutCommand(string RefreshToken) : IRequest;
}
