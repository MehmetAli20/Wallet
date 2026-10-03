using MediatR;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.GetCurrentUser
{
    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, User>
    {
        private readonly IUserRepository _users;
        private readonly ICurrentUser _currentUser;

        public GetCurrentUserQueryHandler(IUserRepository users, ICurrentUser currentUser)
        {
            _users = users;
            _currentUser = currentUser;
        }

        public async Task<User> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
            await _users.GetByIdAsync(_currentUser.UserId, cancellationToken)
                ?? throw new InvalidCredentialsException();
    }
}
