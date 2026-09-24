using MediatR;

namespace Wallet.Application.Users.RefreshSession
{
    public record RefreshSessionCommand(string RefreshToken) : IRequest<IssuedSession>
    {
        public sealed override string ToString() =>
            $"{nameof(RefreshSessionCommand)} {{ RefreshToken = *** }}";
    }
}
