using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Domain.Users
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public UserRole Role { get; private set; }

        private User()
        {
            Username = null!;
            PasswordHash = null!;
        }

        public User(Guid id, string username, string email, string passwordHash, UserRole role)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("User Id cannot be empty.", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be null or whitespace.", nameof(username));
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));
            }
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("Password hash cannot be null or whitespace.", nameof(passwordHash));
            }
            Id = id;
            Username = NormalizeUsername(username);
            Email = NormalizeEmail(email);
            PasswordHash = passwordHash;
            Role = role;
        }

        public static string NormalizeUsername(string username)
        {
            return username.Trim().ToLowerInvariant();
        }
        public static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
