namespace Wallet.Api.Contracts.Users
{
    public record LoginResponse(string Token)
    {
        public sealed override string ToString() =>
            $"{nameof(LoginResponse)} {{ Token = *** }}";
    }
}
