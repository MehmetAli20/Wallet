using MediatR;

namespace Wallet.Application.Users.GetOpenReports
{
    public record GetOpenReportsQuery : IRequest<IReadOnlyList<OpenReport>>;
}
