using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Common;

namespace Wallet.Domain.Accounts
{
    public sealed record LedgerEntry
    {
        public Guid Id { get; }
        public Guid AccountId { get; }
        public LedgerEntryType Type { get; }
        public Money Amount { get; }
        public LedgerEntry(Guid id, Guid accountId, LedgerEntryType type, Money amount)
        {
            if(id == Guid.Empty)
            {
                throw new ArgumentException("Ledger entry Id cannot be empty.", nameof(id));
            }
            if (accountId == Guid.Empty)
            {
                throw new ArgumentException("Account Id cannot be empty.", nameof(accountId));
            }
            if (amount is null)
            {
                throw new ArgumentNullException(nameof(amount), "Amount cannot be null.");
            }
            if (amount.Amount <= 0)
            {
                throw new ArgumentException("Amount must be positive.", nameof(amount));
            }
            Id = id;
            AccountId = accountId;
            Type = type;
            Amount = amount;
        }
    }
}