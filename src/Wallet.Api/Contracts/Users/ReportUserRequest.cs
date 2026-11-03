using Wallet.Domain.Users;

namespace Wallet.Api.Contracts.Users
{
    public record ReportUserRequest(ReportReason Reason, string? Details);
}
