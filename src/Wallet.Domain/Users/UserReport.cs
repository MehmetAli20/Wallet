namespace Wallet.Domain.Users
{
    public class UserReport
    {
        public const int DetailsMaxLength = 500;

        public Guid Id { get; private set; }
        public Guid ReporterId { get; private set; }
        public Guid ReportedUserId { get; private set; }
        public ReportReason Reason { get; private set; }
        public string? Details { get; private set; }
        public DateTimeOffset ReportedAt { get; private set; }
        public DateTimeOffset? ResolvedAt { get; private set; }
        public Guid? ResolvedBy { get; private set; }

        private UserReport() { }

        public UserReport(
            Guid id, Guid reporterId, Guid reportedUserId, ReportReason reason, string? details, DateTimeOffset reportedAt)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Report id cannot be empty.", nameof(id));

            if (reporterId == Guid.Empty)
                throw new ArgumentException("Reporter id cannot be empty.", nameof(reporterId));

            if (reportedUserId == Guid.Empty)
                throw new ArgumentException("Reported user id cannot be empty.", nameof(reportedUserId));

            if (reporterId == reportedUserId)
                throw new ArgumentException("You cannot report yourself.", nameof(reportedUserId));

            Id = id;
            ReporterId = reporterId;
            ReportedUserId = reportedUserId;
            Reason = reason;
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
            ReportedAt = reportedAt.ToUniversalTime();
        }

        public void Resolve(Guid resolvedBy, DateTimeOffset resolvedAt)
        {
            if (ResolvedAt is not null)
                return;

            ResolvedAt = resolvedAt.ToUniversalTime();
            ResolvedBy = resolvedBy;
        }
    }
}
