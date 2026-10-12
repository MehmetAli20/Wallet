namespace Wallet.Application.Users
{
    public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt)
    {
        public override string ToString() =>
            $"{nameof(IssuedAccessToken)} {{ ExpiresAt = {ExpiresAt}, Token = *** }}";
    }
}
