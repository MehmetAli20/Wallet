namespace Wallet.Api.Contracts.Placeholders
{
    public record AddPlaceholderRequest(string DisplayName);

    public record ClaimTokenResponse(string Token);

    public record ClaimPlaceholderRequest(string Token, string Username, string Email, string Password);
}
