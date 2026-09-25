namespace Wallet.Api.Contracts.Users
{
    public record RegisterRequest(string Email, string Password, string DisplayName)
    {
        public sealed override string ToString() =>
            $"{nameof(RegisterRequest)} {{ DisplayName = {DisplayName}, Email = ***, Password = *** }}";
    }
}
