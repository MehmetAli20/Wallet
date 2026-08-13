using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Users
{
    public class AdminUserSeeder
    {
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminUserSeeder> _logger;
        private readonly IPasswordHasher _passwordHasher;

        public AdminUserSeeder(IUserRepository users, IUnitOfWork unitOfWork, ILogger<AdminUserSeeder> logger, IPasswordHasher passwordHasher)
        {
            _users = users;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync(string username, string email, string password, CancellationToken cancellationToken = default)
        {
            var existingUser = await _users.GetByUsernameAsync(username, cancellationToken);
            if(existingUser is not null)
            {
                _logger.LogInformation("Admin user '{Username}' already exists, skipping seed.", existingUser);
                return;
            }

            var admin = new User(
                id: Guid.NewGuid(),
                username: username,
                email: email,
                passwordHash: _passwordHasher.Hash(password),
                role: UserRole.Admin);

            await _users.AddAsync(admin, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin user '{Username}' created.", admin.Username);
        }
    }
}
