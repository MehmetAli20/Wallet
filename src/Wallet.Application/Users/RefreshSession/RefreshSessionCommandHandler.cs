using MediatR;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.RefreshSession
{
    public class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, AuthTokens>
    {
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IUserRepository _users;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefreshSessionCommandHandler> _logger;

        public RefreshSessionCommandHandler(
            IRefreshTokenRepository refreshTokens,
            IUserRepository users,
            IJwtTokenGenerator tokenGenerator,
            IUnitOfWork unitOfWork,
            ILogger<RefreshSessionCommandHandler> logger)
        {
            _refreshTokens = refreshTokens;
            _users = users;
            _tokenGenerator = tokenGenerator;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AuthTokens> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var presented = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken)
                ?? throw new InvalidCredentialsException();

            if (presented.RevokedAt is not null || presented.ExpiresAt <= now)
                throw new InvalidCredentialsException();

            if (presented.UsedAt is not null && !presented.IsWithinReuseInterval(now))
            {
                foreach (var active in await _refreshTokens.GetActiveInFamilyAsync(
                    presented.FamilyId, now, cancellationToken))
                {
                    active.Revoke(now);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}; session family {FamilyId} revoked.",
                    presented.UserId, presented.FamilyId);

                throw new InvalidCredentialsException();
            }

            var user = await _users.GetByIdAsync(presented.UserId, cancellationToken)
                ?? throw new InvalidCredentialsException();

            if (presented.IsActive(now))
                presented.MarkUsed(now);

            var next = presented.Successor(Guid.NewGuid(), now);

            await _refreshTokens.AddAsync(next.RefreshToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthTokens(
                _tokenGenerator.GenerateToken(user),
                next.Token,
                next.RefreshToken.ExpiresAt);
        }
    }
}
