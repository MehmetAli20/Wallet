using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IJwtTokenGenerator tokenGenerator,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _tokenGenerator = tokenGenerator;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            var isLockedOut = user is not null && user.IsLockedOut(now);
            var canSignIn = user is not null && !user.IsPlaceholder && !isLockedOut;

            var passwordHash = canSignIn ? user!.PasswordHash! : _passwordHasher.DummyHash;
            var passwordIsValid = _passwordHasher.Verify(request.Password, passwordHash);

            if (!canSignIn || !passwordIsValid)
            {
                if (user is not null && !user.IsPlaceholder && !isLockedOut)
                {
                    user.RegisterFailedAccess(now);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                _logger.LogWarning("Failed login attempt for username {Username}.", request.Username);

                throw new InvalidCredentialsException();
            }

            user!.ResetAccessFailures();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {Username} logged in.", user.Username);

            return _tokenGenerator.GenerateToken(user);
        }
    }
}
