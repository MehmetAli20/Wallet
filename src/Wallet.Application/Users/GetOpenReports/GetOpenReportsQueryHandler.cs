using MediatR;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.GetOpenReports
{
    public class GetOpenReportsQueryHandler : IRequestHandler<GetOpenReportsQuery, IReadOnlyList<OpenReport>>
    {
        private readonly IUserReportRepository _reports;

        public GetOpenReportsQueryHandler(IUserReportRepository reports)
        {
            _reports = reports;
        }

        public async Task<IReadOnlyList<OpenReport>> Handle(
            GetOpenReportsQuery request, CancellationToken cancellationToken) =>
            await _reports.GetOpenAsync(cancellationToken);
    }
}
