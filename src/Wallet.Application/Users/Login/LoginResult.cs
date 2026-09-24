namespace Wallet.Application.Users.Login
{
    public sealed record LoginResult(string? AccessToken, IssuedSession? Session)
    {
        public override string ToString() =>
            $"{nameof(LoginResult)} {{ AccessToken = ***, Session = {Session} }}";
    }
}
