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

            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            var isRealUser = user is not null && !string.IsNullOrEmpty(user.PasswordHash);

            var recent = isRealUser
                ? await _loginAttempts.GetMostRecentAsync(
                    user!.Id, LoginLockout.MaxFailedAttempts, cancellationToken)
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
                        LoginAttempt.Failure(Guid.NewGuid(), user!.Id, now, request.ClientIp, lockedUntil),
                        cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    if (lockedUntil is not null)
                    {
                        _logger.LogWarning(
                            "User {UserId} locked out until {LockedUntil}.", user.Id, lockedUntil);
                    }
                }

                if (isRealUser)
                {
                    _logger.LogWarning("Failed login attempt for user {UserId}.", user!.Id);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for an unknown username.");
                }

                throw new InvalidCredentialsException();
            }

            await _loginAttempts.AddAsync(
                LoginAttempt.Success(Guid.NewGuid(), user!.Id, now, request.ClientIp), cancellationToken);

            if (request.Purpose == LoginPurpose.AccessToken)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User {UserId} logged in with an access token.", user.Id);

                return new LoginResult(_tokenGenerator.GenerateToken(user), null);
            }

            if (!string.IsNullOrEmpty(request.PreviousSessionToken))
            {
                var previous = await _refreshTokens.GetByTokenAsync(request.PreviousSessionToken, cancellationToken);

                if (previous is not null)
                    await _refreshTokens.RevokeFamilyAsync(previous.FamilyId, now, cancellationToken);
            }

            var session = RefreshToken.StartSession(Guid.NewGuid(), user.Id, now);

            await _refreshTokens.AddAsync(session.RefreshToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} started a browser session.", user.Id);

            return new LoginResult(null, new IssuedSession(session.Token, session.RefreshToken.ExpiresAt));
        }
    }
}
