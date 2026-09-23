using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IUnitOfWork _unitOfWork;

        public LogoutCommandHandler(IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork)
        {
            _refreshTokens = refreshTokens;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var presented = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

            if (presented is null)
                return;

            foreach (var active in await _refreshTokens.GetActiveInFamilyAsync(
                presented.FamilyId, now, cancellationToken))
            {
                active.Revoke(now);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
