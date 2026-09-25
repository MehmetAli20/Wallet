namespace Wallet.Api.Contracts.Users
{
    public record LoginRequest(string Email, string Password)
    {
        public sealed override string ToString() =>
            $"{nameof(LoginRequest)} {{ Email = ***, Password = *** }}";
    }
}
