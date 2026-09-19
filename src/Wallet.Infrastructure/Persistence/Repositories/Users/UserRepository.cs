using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Wallet.Infrastructure.Persistence.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly WalletDbContext _context;

        public UserRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetPlaceholderInMyGroupsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.Id == id && u.IsPlaceholder)
                .Where(u => _context.Groups.Any(g => g.Members.Any(m => m.UserId == u.Id)))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> DisplayNameTakenInGroupAsync(
            Guid groupId, string displayName, CancellationToken cancellationToken = default)
        {
            var normalized = displayName.Trim().ToLower();

            return await _context.Users
                .Where(u => _context.Groups.Any(g => g.Id == groupId && g.Members.Any(m => m.UserId == u.Id)))
                .AnyAsync(u => u.DisplayName.ToLower() == normalized, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var normalizedUsername = User.NormalizeUsername(username);
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
            //AddAsync'in async olmasının tek sebebi özel değer üreteçleridir
            //(ör. SQL Server'ın HiLo stratejisi — bir sonraki id bloğunu almak için DB'ye gitmesi gerekir).
            //Bizde Guid id'yi ben üretiyorum (Guid.NewGuid()),
            //yani DB'ye gitmeye gerek yok — senkron Add de tamamen yeterliydi.
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = User.NormalizeEmail(email);
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(u=>u.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetPlaceholdersInGroupAsync(
            Guid groupId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.IsPlaceholder)
                .Where(u => _context.Groups.Any(g => g.Id == groupId && g.Members.Any(m => m.UserId == u.Id)))
                .OrderBy(u => u.DisplayName)
                .ToListAsync(cancellationToken);
        }
    }
}