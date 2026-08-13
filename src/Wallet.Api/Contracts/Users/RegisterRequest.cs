namespace Wallet.Api.Contracts.Users
{
    public record RegisterRequest(string Username, string Email, string Password);
}
