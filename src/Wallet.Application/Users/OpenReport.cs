using Wallet.Domain.Users;

namespace Wallet.Application.Users
{
    public sealed record OpenReport(
        Guid Id,
        Guid ReporterId,
        string ReporterDisplayName,
        Guid ReportedUserId,
        string ReportedDisplayName,
        ReportReason Reason,
        string? Details,
        DateTimeOffset ReportedAt);
}
