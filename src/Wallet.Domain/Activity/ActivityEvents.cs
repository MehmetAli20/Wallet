using Wallet.Domain.Common;

namespace Wallet.Domain.Activity
{
    public enum ActivityType
    {
        ExpenseCreated = 1,
        ExpenseRevised = 2,
        ExpenseReversed = 3,
        SettlementRecorded = 4,
        MemberInvited = 5,
        MemberJoined = 6,
        InvitationDeclined = 7,
        MemberRemoved = 8
    }

    public record ExpenseCreated(
        Guid ExpenseId,
        Guid GroupId,
        Guid CreatedBy,
        Guid PayerId,
        decimal Amount,
        string Currency,
        string Description,
        bool IsRevision,
        DateTimeOffset OccurredAt) : IDomainEvent;

    public record ExpenseReversed(
        Guid ExpenseId,
        Guid GroupId,
        Guid ReversedBy,
        decimal Amount,
        string Currency,
        string Description,
        string? Reason,
        DateTimeOffset OccurredAt) : IDomainEvent;

    public record SettlementRecorded(
        Guid SettlementId,
        Guid GroupId,
        Guid PayerId,
        Guid PayeeId,
        decimal Amount,
        string Currency,
        DateTimeOffset OccurredAt) : IDomainEvent;

    public record MemberInvited(
        Guid GroupId,
        Guid InvitedUserId,
        Guid InvitedBy,
        DateTimeOffset OccurredAt) : IDomainEvent;

    public record MemberJoined(
        Guid GroupId,
        Guid UserId,
        DateTimeOffset OccurredAt) : IDomainEvent;

    public record InvitationDeclined(
        Guid GroupId,
        Guid UserId,
        DateTimeOffset OccurredAt) : IDomainEvent;

    public record MemberRemoved(
        Guid GroupId,
        Guid UserId,
        Guid RemovedBy,
        DateTimeOffset OccurredAt) : IDomainEvent;
}
