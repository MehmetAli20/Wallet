namespace Wallet.Api.Contracts.Placeholders
{
    public record AddPlaceholderRequest(string DisplayName);

    public record ClaimTokenResponse(string Token)
    {
        public sealed override string ToString() =>
            $"{nameof(ClaimTokenResponse)} {{ Token = *** }}";
    }

    public record ClaimPlaceholderRequest(string Token, string Username, string Email, string Password)
    {
        public sealed override string ToString() =>
            $"{nameof(ClaimPlaceholderRequest)} {{ Username = {Username}, Token = ***, Email = ***, Password = *** }}";
    }
}
