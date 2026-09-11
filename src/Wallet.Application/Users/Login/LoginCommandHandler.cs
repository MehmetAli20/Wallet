using MediatR;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginAttemptRepository _loginAttempts;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserRepository userRepository,
            ILoginAttemptRepository loginAttempts,
            IJwtTokenGenerator tokenGenerator,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _loginAttempts = loginAttempts;
            _tokenGenerator = tokenGenerator;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
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

                _logger.LogWarning("Failed login attempt for username {Username}.", request.Username);

                throw new InvalidCredentialsException();
            }

            await _loginAttempts.AddAsync(
                LoginAttempt.Success(Guid.NewGuid(), user!.Id, now, request.ClientIp), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {Username} logged in.", user.Username);

            return _tokenGenerator.GenerateToken(user);
        }
    }
}
