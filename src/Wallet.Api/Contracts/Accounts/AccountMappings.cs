using Wallet.Api.Contracts.Accounts.Responses;
using Wallet.Domain.Accounts;

namespace Wallet.Api.Contracts.Accounts
{
    public static class AccountMappings
    {
        public static AccountResponse ToResponse(this Account account) =>
            new AccountResponse(account.Id,
                                account.Balance.Amount,
                                account.Balance.Currency,
                                account.Entries.Select(entry => entry.ToResponse()).ToList());

        public static LedgerEntryResponse ToResponse(this LedgerEntry entry) =>
            new(
                entry.Id,
                entry.Type.ToString(),
                entry.Amount.Amount,
                entry.Amount.Currency,
                entry.Sequence,
                entry.OccurredAt);

        public static AccountListResponse ToListResponse(this Account account) => 
            new(account.Id, 
                account.Balance.Amount, 
                account.Balance.Currency);
    }
}