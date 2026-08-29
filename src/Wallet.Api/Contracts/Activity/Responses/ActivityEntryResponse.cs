using Wallet.Domain.Activity;

namespace Wallet.Api.Contracts.Activity.Responses
{
    public record ActivityEntryResponse(
        Guid Id,
        long Sequence,
        Guid GroupId,
        Guid ActorId,
        string Type,
        Guid? SubjectId,
        decimal? Amount,
        string? Currency,
        string? Description,
        DateTimeOffset OccurredAt);

    public record MarkActivitySeenRequest(long Sequence);

    public record GroupUnreadCountResponse(Guid GroupId, int Unread);

    public static class ActivityMappings
    {
        public static ActivityEntryResponse ToResponse(this ActivityEntry entry) =>
            new(
                entry.Id,
                entry.Sequence,
                entry.GroupId,
                entry.ActorId,
                entry.Type.ToString(),
                entry.SubjectId,
                entry.Amount,
                entry.Currency,
                entry.Description,
                entry.OccurredAt);

        public static GroupUnreadCountResponse ToResponse(this GroupUnreadCount count) =>
            new(count.GroupId, count.Unread);
    }
}
