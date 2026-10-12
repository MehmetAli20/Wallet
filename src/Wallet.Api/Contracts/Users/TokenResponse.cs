namespace Wallet.Api.Contracts.Users
{
    public record TokenResponse(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt)
    {
        public sealed override string ToString() =>
            $"{nameof(TokenResponse)} {{ AccessTokenExpiresAt = {AccessTokenExpiresAt}, RefreshTokenExpiresAt = {RefreshTokenExpiresAt}, AccessToken = ***, RefreshToken = *** }}";
    }
}
