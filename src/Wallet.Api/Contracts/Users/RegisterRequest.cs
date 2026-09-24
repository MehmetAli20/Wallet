namespace Wallet.Api.Contracts.Users
{
    public record RegisterRequest(string Username, string Email, string Password)
    {
        public sealed override string ToString() =>
            $"{nameof(RegisterRequest)} {{ Username = {Username}, Email = ***, Password = *** }}";
    }
}
