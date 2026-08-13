using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IJwtTokenGenerator tokenGenerator,
            IPasswordHasher passwordHasher,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _tokenGenerator = tokenGenerator;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            var passwordHash = user?.PasswordHash ?? _passwordHasher.DummyHash;
            var passwordIsValid = _passwordHasher.Verify(request.Password, passwordHash);

            if (user is null || !passwordIsValid)
            {
                _logger.LogWarning("Failed login attempt for username {Username}.", request.Username);

                throw new InvalidCredentialsException();
            }

            _logger.LogInformation("User {Username} logged in.", user.Username);

            return _tokenGenerator.GenerateToken(user);
        }
    }
}
