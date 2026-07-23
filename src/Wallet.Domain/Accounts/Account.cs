using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Common;

namespace Wallet.Domain.Accounts
{
    public class Account
    {
        public Guid Id { get; private set; }
        public Money Balance { get; private set; }
        public Account(Guid id, Money openingBalance)
        {
            if(id == Guid.Empty)
            {
                throw new ArgumentException("Account Id cannot be empty.", nameof(id));
            }
            
            if(openingBalance is null)
            {
                throw new ArgumentNullException(nameof(openingBalance), "Opening balance cannot be null.");
            }
            if(openingBalance.Amount < 0)
            {
                throw new ArgumentException("Opening balance cannot be negative.", nameof(openingBalance));
            }

            Id = id;
            Balance = openingBalance;
        }

        public void Deposit(Money amount)
        {
            if (amount is null)
            {
                throw new ArgumentNullException(nameof(amount), "Deposit amount cannot be null.");
            }
            if(amount.Amount <= 0)
            {
                throw new ArgumentException("Deposit amount must be positive", nameof(amount));
            }

            Balance = Balance.Add(amount);
            _entries.Add(new LedgerEntry(Guid.NewGuid(), Id, LedgerEntryType.Credit, amount));
        }
        
        public void Withdraw(Money amount)
        {
            if (amount is null)
            {
                throw new ArgumentNullException(nameof(amount), "Withdrawal amount cannot be null.");
            }
            if (amount.Amount <= 0)
            {
                throw new ArgumentException("Withdrawal amount must be positive", nameof(amount));
            }
            if(amount.Currency != Balance.Currency)
            {
                throw new InvalidOperationException("Currency mismatch between withdrawal amount and account balance.");
            }
            if (amount.Amount > Balance.Amount)
            {
                throw new InvalidOperationException("Insufficient funds for withdrawal.");
            }


            Balance = Balance.Subtract(amount);
            _entries.Add(new LedgerEntry(Guid.NewGuid(), Id, LedgerEntryType.Debit, amount));
        }

        private readonly List<LedgerEntry> _entries = new();
        public IReadOnlyList<LedgerEntry> Entries => _entries.AsReadOnly();
    }
}
