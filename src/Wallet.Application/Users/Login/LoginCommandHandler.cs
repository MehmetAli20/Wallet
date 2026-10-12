using MediatR;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginAttemptRepository _loginAttempts;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserRepository userRepository,
            ILoginAttemptRepository loginAttempts,
            IRefreshTokenRepository refreshTokens,
            IJwtTokenGenerator tokenGenerator,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _loginAttempts = loginAttempts;
            _refreshTokens = refreshTokens;
            _tokenGenerator = tokenGenerator;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            var isRealUser = user is not null && !string.IsNullOrEmpty(user.PasswordHash);

            var fromFamiliarNetwork = isRealUser && ClientNetwork.IsFamiliar(
                request.ClientIp,
                await _loginAttempts.GetSuccessfulClientIpsAsync(
                    user!.Id, now - LoginLockout.FamiliarityWindow, cancellationToken));

            var recent = isRealUser
                ? await _loginAttempts.GetMostRecentAsync(
                    user!.Id, fromFamiliarNetwork, LoginLockout.MaxFailedAttempts, cancellationToken)
                : [];

            var isLockedOut = LoginLockout.IsLockedOut(recent, now);
            var canSignIn = isRealUser && !isLockedOut;

            var passwordHash = canSignIn ? user!.PasswordHash! : _passwordHasher.DummyHash;
            var passwordIsValid = _passwordHasher.Verify(request.Password, passwordHash);

            if (!canSignIn || !passwordIsValid)
            {
                if (isRealUser && !isLockedOut)
                {
                    var lockedUntil = LoginLockout.LockoutForNextFailure(recent, now);

                    await _loginAttempts.AddAsync(
                        LoginAttempt.Failure(
                            Guid.NewGuid(), user!.Id, now, request.ClientIp, lockedUntil, fromFamiliarNetwork),
                        cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    if (lockedUntil is not null)
                    {
                        _logger.LogWarning(
                            "User {UserId} locked out from {Networks} networks until {LockedUntil}.",
                            user.Id, fromFamiliarNetwork ? "familiar" : "unfamiliar", lockedUntil);
                    }
                }

                if (isRealUser)
                {
                    _logger.LogWarning("Failed login attempt for user {UserId}.", user!.Id);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for an unknown account.");
                }

                throw new InvalidCredentialsException();
            }

            await _loginAttempts.AddAsync(
                LoginAttempt.Success(Guid.NewGuid(), user!.Id, now, request.ClientIp, fromFamiliarNetwork),
                cancellationToken);

            if (request.Purpose == LoginPurpose.BrowserSession && !string.IsNullOrEmpty(request.PreviousSessionToken))
            {
                var previous = await _refreshTokens.GetByTokenAsync(request.PreviousSessionToken, cancellationToken);

                if (previous is not null)
                    await _refreshTokens.RevokeFamilyAsync(previous.FamilyId, now, cancellationToken);
            }

            var session = RefreshToken.StartSession(Guid.NewGuid(), user.Id, now);

            await _refreshTokens.AddAsync(session.RefreshToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var issuedSession = new IssuedSession(session.Token, session.RefreshToken.ExpiresAt);

            if (request.Purpose == LoginPurpose.BrowserSession)
            {
                _logger.LogInformation("User {UserId} started a browser session.", user.Id);

                return new LoginResult(null, issuedSession);
            }

            _logger.LogInformation("User {UserId} started an app session.", user.Id);

            return new LoginResult(_tokenGenerator.GenerateToken(user, session.RefreshToken.FamilyId), issuedSession);
        }
    }
}
