namespace Wallet.Api.Contracts.Users
{
    public record RegisterRequest(string Username, string Email, string Password, string DisplayName)
    {
        public sealed override string ToString() =>
            $"{nameof(RegisterRequest)} {{ Username = {Username}, DisplayName = {DisplayName}, Email = ***, Password = *** }}";
    }
}
