using MediatR;
using Wallet.Application.Users.Login;

namespace Wallet.Application.Users.RefreshSession
{
    public record RefreshSessionCommand(string RefreshToken, LoginPurpose Purpose = LoginPurpose.BrowserSession)
        : IRequest<LoginResult>
    {
        public sealed override string ToString() =>
            $"{nameof(RefreshSessionCommand)} {{ Purpose = {Purpose}, RefreshToken = *** }}";
    }
}
