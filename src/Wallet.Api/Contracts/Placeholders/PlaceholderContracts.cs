namespace Wallet.Api.Contracts.Placeholders
{
    public record AddPlaceholderRequest(string DisplayName);

    public record ClaimTokenResponse(string Token)
    {
        public sealed override string ToString() =>
            $"{nameof(ClaimTokenResponse)} {{ Token = *** }}";
    }

    public record ClaimPlaceholderRequest(string Token, string Email, string Password)
    {
        public sealed override string ToString() =>
            $"{nameof(ClaimPlaceholderRequest)} {{ Token = ***, Email = ***, Password = *** }}";
    }
}
