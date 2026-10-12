namespace Wallet.Api.Contracts.Users
{
    public record RefreshTokenRequest(string RefreshToken)
    {
        public sealed override string ToString() =>
            $"{nameof(RefreshTokenRequest)} {{ RefreshToken = *** }}";
    }
}
