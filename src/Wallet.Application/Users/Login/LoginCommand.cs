using MediatR;

namespace Wallet.Application.Users.Login
{
    public record LoginCommand(
        string Username,
        string Password,
        string? ClientIp = null,
        LoginPurpose Purpose = LoginPurpose.AccessToken,
        string? PreviousSessionToken = null) : IRequest<LoginResult>
    {
        public sealed override string ToString() =>
            $"{nameof(LoginCommand)} {{ Username = {Username}, ClientIp = {ClientIp}, Purpose = {Purpose}, Password = ***, PreviousSessionToken = *** }}";
    }
}
