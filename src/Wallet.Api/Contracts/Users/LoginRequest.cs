namespace Wallet.Api.Contracts.Users
{
    public record LoginRequest(string Username, string Password)
    {
        public sealed override string ToString() =>
            $"{nameof(LoginRequest)} {{ Username = {Username}, Password = *** }}";
    }
}
