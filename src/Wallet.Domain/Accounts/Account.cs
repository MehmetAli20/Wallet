using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;

namespace Wallet.Domain.Accounts
{
    public class Account
    {
        private readonly List<LedgerEntry> _entries = new();

        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }
        public Money Balance { get; private set; }
        public IReadOnlyList<LedgerEntry> Entries => _entries.AsReadOnly();

        public Account(Guid id, Guid ownerId, Money openingBalance)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Account Id cannot be empty.", nameof(id));
            }

            if (ownerId == Guid.Empty)
            {
                throw new ArgumentException("Owner Id cannot be empty.", nameof(ownerId));
            }

            if (openingBalance is null)
            {
                throw new ArgumentNullException(nameof(openingBalance), "Opening balance cannot be null.");
            }

            if (openingBalance.Amount < 0)
            {
                throw new ArgumentException("Opening balance cannot be negative.", nameof(openingBalance));
            }

            Id = id;
            OwnerId = ownerId;
            Balance = openingBalance;
        }

        private Account()
        {
            Balance = null!;
        }

        public void Deposit(Money amount)
        {
            if (amount is null)
            {
                throw new ArgumentNullException(nameof(amount), "Deposit amount cannot be null.");
            }

            if (amount.Amount <= 0)
            {
                throw new ArgumentException("Deposit amount must be positive", nameof(amount));
            }

            Balance = Balance.Add(amount);
            RecordEntry(LedgerEntryType.Credit, amount);
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

            if (amount.Amount > Balance.Amount)
            {
                throw new InsufficientFundsException(Id);
            }

            Balance = Balance.Subtract(amount);
            RecordEntry(LedgerEntryType.Debit, amount);
        }

        private void RecordEntry(LedgerEntryType type, Money amount)
        {
            _entries.Add(new LedgerEntry(
                Guid.NewGuid(),
                Id,
                type,
                amount,
                DateTimeOffset.UtcNow,
                _entries.Count));
        }
    }
}