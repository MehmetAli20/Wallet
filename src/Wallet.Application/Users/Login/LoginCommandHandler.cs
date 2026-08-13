using MediatR;
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

        public LoginCommandHandler(IUserRepository userRepository, IJwtTokenGenerator tokenGenerator, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _tokenGenerator = tokenGenerator;
            _passwordHasher = passwordHasher;
        }

        public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            var passwordHash = user?.PasswordHash ?? _passwordHasher.DummyHash;
            var passwordIsValid = _passwordHasher.Verify(request.Password, passwordHash);

            if (user is null || !passwordIsValid)
            {
                throw new InvalidCredentialsException();
            }

            return _tokenGenerator.GenerateToken(user);
        }
    }
}
