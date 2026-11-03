using MediatR;

namespace Wallet.Application.Users.ResolveReport
{
    public record ResolveReportCommand(Guid ReportId) : IRequest;
}
