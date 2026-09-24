namespace Wallet.Application.Users
{
    public sealed record IssuedSession(string Token, DateTimeOffset ExpiresAt)
    {
        public override string ToString() =>
            $"{nameof(IssuedSession)} {{ ExpiresAt = {ExpiresAt}, Token = *** }}";
    }
}
