namespace Wallet.Application.Users.Login
{
    public sealed record LoginResult(IssuedAccessToken? AccessToken, IssuedSession? Session)
    {
        public override string ToString() =>
            $"{nameof(LoginResult)} {{ AccessToken = {AccessToken}, Session = {Session} }}";
    }
}
