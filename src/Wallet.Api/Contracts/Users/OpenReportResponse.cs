namespace Wallet.Api.Contracts.Users
{
    public record OpenReportResponse(
        Guid Id,
        Guid ReporterId,
        string ReporterDisplayName,
        Guid ReportedUserId,
        string ReportedDisplayName,
        string Reason,
        string? Details,
        DateTimeOffset ReportedAt);
}
