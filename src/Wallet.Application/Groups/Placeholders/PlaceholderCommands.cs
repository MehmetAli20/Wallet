using FluentValidation;
using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Users;

namespace Wallet.Application.Groups.Placeholders
{
    public record AddPlaceholderCommand(Guid GroupId, string DisplayName) : IRequest<Guid>;

    public class AddPlaceholderCommandValidator : AbstractValidator<AddPlaceholderCommand>
    {
        public AddPlaceholderCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        }
    }

    public class AddPlaceholderCommandHandler : IRequestHandler<AddPlaceholderCommand, Guid>
    {
        private readonly IGroupRepository _groups;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public AddPlaceholderCommandHandler(
            IGroupRepository groups,
            IUserRepository users,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _groups = groups;
            _users = users;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(AddPlaceholderCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            var displayName = request.DisplayName.Trim();

            var taken = await _users.DisplayNameTakenInGroupAsync(
                request.GroupId, displayName, cancellationToken);

            if (taken)
                throw new InvalidGroupOperationException(
                    $"'{displayName}' is already used in this group. Pick a name that tells them apart.");

            var placeholder = User.CreatePlaceholder(Guid.NewGuid(), displayName);
            await _users.AddAsync(placeholder, cancellationToken);

            group.AddPlaceholder(placeholder.Id, _currentUser.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return placeholder.Id;
        }
    }
}
