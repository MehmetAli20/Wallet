using MediatR;

namespace Wallet.Application.Users.Login
{
    public record LoginCommand(
        string Email,
        string Password,
        string? ClientIp = null,
        LoginPurpose Purpose = LoginPurpose.AccessToken,
        string? PreviousSessionToken = null) : IRequest<LoginResult>
    {
        public sealed override string ToString() =>
            $"{nameof(LoginCommand)} {{ ClientIp = {ClientIp}, Purpose = {Purpose}, Email = ***, Password = ***, PreviousSessionToken = *** }}";
    }
}
