using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.RenameCurrentUser
{
    public class RenameCurrentUserCommandHandler : IRequestHandler<RenameCurrentUserCommand, User>
    {
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public RenameCurrentUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _users = users;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<User> Handle(RenameCurrentUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken)
                ?? throw new InvalidCredentialsException();

            var displayName = User.NormalizeDisplayName(request.DisplayName);

            var keepsTheSameName = User.TryNormalizeDisplayName(user.DisplayName, out var current)
                && User.DisplayNameKey(current) == User.DisplayNameKey(displayName);

            if (!keepsTheSameName
                && await _users.DisplayNameTakenByGroupmateAsync(user.Id, displayName, cancellationToken))
            {
                throw new DisplayNameTakenException(displayName);
            }

            user.Rename(displayName);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return user;
        }
    }
}
