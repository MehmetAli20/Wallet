using MediatR;

namespace Wallet.Application.Users.RefreshSession
{
    public record RefreshSessionCommand(string RefreshToken) : IRequest<AuthTokens>;
}
