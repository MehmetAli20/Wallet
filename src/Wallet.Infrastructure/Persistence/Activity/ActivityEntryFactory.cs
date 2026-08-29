using Wallet.Domain.Activity;
using Wallet.Domain.Common;

namespace Wallet.Infrastructure.Persistence.Activity
{
    public static class ActivityEntryFactory
    {
        public static ActivityEntry? From(IDomainEvent domainEvent) => domainEvent switch
        {
            ExpenseCreated e => new ActivityEntry(
                Guid.NewGuid(), e.GroupId, e.CreatedBy,
                e.IsRevision ? ActivityType.ExpenseRevised : ActivityType.ExpenseCreated,
                e.OccurredAt, e.ExpenseId, e.Amount, e.Currency, e.Description),

            ExpenseReversed e => new ActivityEntry(
                Guid.NewGuid(), e.GroupId, e.ReversedBy, ActivityType.ExpenseReversed,
                e.OccurredAt, e.ExpenseId, e.Amount, e.Currency, e.Reason ?? e.Description),

            SettlementRecorded e => new ActivityEntry(
                Guid.NewGuid(), e.GroupId, e.PayerId, ActivityType.SettlementRecorded,
                e.OccurredAt, e.SettlementId, e.Amount, e.Currency),

            MemberInvited e => new ActivityEntry(
                Guid.NewGuid(), e.GroupId, e.InvitedBy, ActivityType.MemberInvited,
                e.OccurredAt, e.InvitedUserId),

            MemberJoined e => new ActivityEntry(
                Guid.NewGuid(), e.GroupId, e.UserId, ActivityType.MemberJoined,
                e.OccurredAt, e.UserId),

            InvitationDeclined e => new ActivityEntry(
                Guid.NewGuid(), e.GroupId, e.UserId, ActivityType.InvitationDeclined,
                e.OccurredAt, e.UserId),

            MemberRemoved e => new ActivityEntry(
                Guid.NewGuid(), e.GroupId, e.RemovedBy, ActivityType.MemberRemoved,
                e.OccurredAt, e.UserId),

            _ => null
        };
    }
}
