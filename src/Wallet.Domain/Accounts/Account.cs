using Wallet.Domain.Common;

namespace Wallet.Domain.Accounts
{
    public class Account
    {
        private readonly List<LedgerEntry> _entries = new();
        private decimal _balanceAmount;

        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }

        public string Currency { get; private set; }

        public Money Balance => new(_balanceAmount, Currency);

        public IReadOnlyList<LedgerEntry> Entries => _entries.AsReadOnly();

        public Account(Guid id, Guid ownerId, string currency)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Account Id cannot be empty.", nameof(id));
            }

            if (ownerId == Guid.Empty)
            {
                throw new ArgumentException("Owner Id cannot be empty.", nameof(ownerId));
            }

            Id = id;
            OwnerId = ownerId;
            Currency = Money.NormalizeCurrency(currency);
            _balanceAmount = 0m;
        }

        private Account()
        {
            Currency = null!;
        }

        public void Credit(Money amount)
        {
            if (amount is null)
            {
                throw new ArgumentNullException(nameof(amount), "Credit amount cannot be null.");
            }

            if (amount.Amount <= 0)
            {
                throw new ArgumentException("Credit amount must be positive", nameof(amount));
            }

            _balanceAmount = Balance.Add(amount).Amount;
            RecordEntry(LedgerEntryType.Credit, amount);
        }

        public void Debit(Money amount)
        {
            if (amount is null)
            {
                throw new ArgumentNullException(nameof(amount), "Debit amount cannot be null.");
            }

            if (amount.Amount <= 0)
            {
                throw new ArgumentException("Debit amount must be positive", nameof(amount));
            }

            _balanceAmount = Balance.Subtract(amount).Amount;
            RecordEntry(LedgerEntryType.Debit, amount);
        }

        private void RecordEntry(LedgerEntryType type, Money amount)
        {
            _entries.Add(new LedgerEntry(
                Guid.NewGuid(),
                Id,
                OwnerId,
                type,
                amount,
                DateTimeOffset.UtcNow,
                _entries.Count));
        }
    }
}