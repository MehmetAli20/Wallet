namespace Wallet.Application.Users
{
    public sealed record AuthTokens(
        string AccessToken,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt);
}
