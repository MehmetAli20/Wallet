using BCrypt.Net;
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

        public LoginCommandHandler(IUserRepository userRepository, IJwtTokenGenerator tokenGenerator)
        {
            _userRepository = userRepository;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) 
            {
                throw new InvalidCredentialsException();
            }

            return _tokenGenerator.GenerateToken(user);

        }
    }
}
