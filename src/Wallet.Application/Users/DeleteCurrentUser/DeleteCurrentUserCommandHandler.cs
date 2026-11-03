using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.DeleteCurrentUser
{
    public class DeleteCurrentUserCommandHandler : IRequestHandler<DeleteCurrentUserCommand>
    {
        private readonly IUserRepository _users;
        private readonly IGroupRepository _groups;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public DeleteCurrentUserCommandHandler(
            IUserRepository users,
            IGroupRepository groups,
            IRefreshTokenRepository refreshTokens,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _users = users;
            _groups = groups;
            _refreshTokens = refreshTokens;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(DeleteCurrentUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken)
                ?? throw new InvalidCredentialsException();

            if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new PasswordConfirmationFailedException();

            foreach (var group in await _groups.GetAllForPartyAsync(user.Id, cancellationToken))
            {
                var placeholders = await _users.GetPlaceholdersInGroupAsync(group.Id, cancellationToken);

                group.RemoveDeletedUser(user.Id, placeholders.Select(p => p.Id).ToList());
            }

            user.Delete();

            await _refreshTokens.RevokeAllForUserAsync(user.Id, DateTimeOffset.UtcNow, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
