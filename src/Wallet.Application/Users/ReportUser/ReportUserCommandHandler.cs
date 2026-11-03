using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.ReportUser
{
    public class ReportUserCommandHandler : IRequestHandler<ReportUserCommand>
    {
        private readonly IUserRepository _users;
        private readonly IUserReportRepository _reports;
        private readonly IGroupRepository _groups;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public ReportUserCommandHandler(
            IUserRepository users,
            IUserReportRepository reports,
            IGroupRepository groups,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _users = users;
            _reports = reports;
            _groups = groups;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(ReportUserCommand request, CancellationToken cancellationToken)
        {
            var reporterId = _currentUser.UserId;

            var target = await _users.GetByIdAsync(request.UserId, cancellationToken);

            if (target is null
                || target.IsDeleted
                || !await _groups.EverSharedAGroupAsync(reporterId, request.UserId, cancellationToken))
                throw new UserNotFoundException(request.UserId);

            if (await _reports.HasOpenReportAsync(reporterId, request.UserId, cancellationToken))
                return;

            await _reports.AddAsync(
                new UserReport(
                    Guid.NewGuid(), reporterId, request.UserId, request.Reason, request.Details, DateTimeOffset.UtcNow),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
