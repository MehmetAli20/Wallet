using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken = default)
        {
            var existingUsername = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if(existingUsername is not null)
            {
                throw new UsernameAlreadyExistsException();
            }
            
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if(existingEmail is not null)
            {
                throw new EmailAlreadyExistsException();
            }

            var user = new User(
                id: Guid.NewGuid(),
                username: request.Username,
                email: request.Email,
                passwordHash: _passwordHasher.Hash(request.Password),
                role: UserRole.User,
                displayName: request.DisplayName);

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}