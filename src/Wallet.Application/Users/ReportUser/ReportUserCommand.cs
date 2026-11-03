using MediatR;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.ReportUser
{
    public record ReportUserCommand(Guid UserId, ReportReason Reason, string? Details) : IRequest;
}
