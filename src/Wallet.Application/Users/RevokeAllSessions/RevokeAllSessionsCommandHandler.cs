using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.RevokeAllSessions
{
    public class RevokeAllSessionsCommandHandler : IRequestHandler<RevokeAllSessionsCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public RevokeAllSessionsCommandHandler(
            IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _refreshTokens = refreshTokens;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(RevokeAllSessionsCommand request, CancellationToken cancellationToken)
        {
            await _refreshTokens.RevokeAllForUserAsync(_currentUser.UserId, DateTimeOffset.UtcNow, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
