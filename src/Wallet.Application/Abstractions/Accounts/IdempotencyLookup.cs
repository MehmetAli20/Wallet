namespace Wallet.Application.Abstractions.Accounts
{
    public record IdempotencyLookup(bool Exists, string? Response);
}
