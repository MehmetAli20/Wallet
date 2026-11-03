using Wallet.Application.Users;
using Wallet.Domain.Users;

namespace Wallet.Api.Contracts.Users
{
    public static class UserMappings
    {
        public static CurrentUserResponse ToCurrentUserResponse(this User user) =>
            new(user.Id, user.DisplayName);

        public static BlockedUserResponse ToBlockedUserResponse(this BlockedUser blocked) =>
            new(blocked.UserId, blocked.DisplayName);

        public static OpenReportResponse ToOpenReportResponse(this OpenReport report) =>
            new(report.Id, report.ReporterId, report.ReporterDisplayName, report.ReportedUserId,
                report.ReportedDisplayName, report.Reason.ToString(), report.Details, report.ReportedAt);
    }
}
