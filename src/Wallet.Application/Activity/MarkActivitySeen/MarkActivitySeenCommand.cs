using FluentValidation;
using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Activity;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Activity;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Activity.MarkActivitySeen
{
    public record MarkActivitySeenCommand(Guid GroupId, long Sequence) : IRequest;

    public class MarkActivitySeenCommandValidator : AbstractValidator<MarkActivitySeenCommand>
    {
        public MarkActivitySeenCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.Sequence).GreaterThanOrEqualTo(0);
        }
    }

    public class MarkActivitySeenCommandHandler : IRequestHandler<MarkActivitySeenCommand>
    {
        private readonly IGroupRepository _groups;
        private readonly IActivityRepository _activity;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public MarkActivitySeenCommandHandler(
            IGroupRepository groups,
            IActivityRepository activity,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _groups = groups;
            _activity = activity;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(MarkActivitySeenCommand request, CancellationToken cancellationToken)
        {
            _ = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            var cursor = await _activity.GetReadCursorAsync(request.GroupId, cancellationToken);

            if (cursor is null)
            {
                cursor = new GroupActivityRead(_currentUser.UserId, request.GroupId, request.Sequence);
                await _activity.AddReadCursorAsync(cursor, cancellationToken);
            }
            else
            {
                cursor.Advance(request.Sequence);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
