using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Users.ResolveReport
{
    public class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand>
    {
        private readonly IUserReportRepository _reports;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public ResolveReportCommandHandler(
            IUserReportRepository reports, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _reports = reports;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(ResolveReportCommand request, CancellationToken cancellationToken)
        {
            var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
                ?? throw new ReportNotFoundException(request.ReportId);

            report.Resolve(_currentUser.UserId, DateTimeOffset.UtcNow);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
