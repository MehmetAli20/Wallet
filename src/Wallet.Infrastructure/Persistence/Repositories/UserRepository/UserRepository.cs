using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Wallet.Infrastructure.Persistence.Repositories.UserRepository
{
    public class UserRepository : IUserRepository
    {
        private readonly WalletDbContext _context;

        public UserRepository(WalletDbContext context)
        {
            _context = context;
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
    }
}
