using Wallet.Domain.Accounts;
using Wallet.Domain.Common;

public sealed record LedgerEntry
{
    public Guid Id { get; }
    public Guid AccountId { get; }
    public Guid OwnerId { get; }
    public LedgerEntryType Type { get; }
    public Money Amount { get; }
    public DateTimeOffset OccurredAt { get; }
    public int Sequence { get; }

    public LedgerEntry(
        Guid id,
        Guid accountId,
        Guid ownerId,
        LedgerEntryType type,
        Money amount,
        DateTimeOffset occurredAt,
        int sequence)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Ledger entry Id cannot be empty.", nameof(id));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account Id cannot be empty.", nameof(accountId));

        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner Id cannot be empty.", nameof(ownerId));

        if (amount is null)
            throw new ArgumentNullException(nameof(amount), "Amount cannot be null.");

        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        if (occurredAt == default)
            throw new ArgumentException("OccurredAt must be set.", nameof(occurredAt));

        if (sequence < 0)
            throw new ArgumentException("Sequence cannot be negative.", nameof(sequence));

        Id = id;
        AccountId = accountId;
        OwnerId = ownerId;
        Type = type;
        Amount = amount;
        OccurredAt = occurredAt;
        Sequence = sequence;
    }

    private LedgerEntry()
    {
        Amount = null!;
    }
}