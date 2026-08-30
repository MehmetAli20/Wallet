using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Domain.Users
{
    public class User
    {
        public Guid Id { get; private set; }
        public string DisplayName { get; private set; }
        public string? Username { get; private set; }
        public string? Email { get; private set; }
        public string? PasswordHash { get; private set; }
        public UserRole Role { get; private set; }
        public bool IsPlaceholder { get; private set; }

        private User()
        {
            DisplayName = null!;
        }

        public User(Guid id, string username, string email, string passwordHash, UserRole role)
            : this(id, username, email, passwordHash, role, displayName: null)
        {
        }

        public User(Guid id, string username, string email, string passwordHash, UserRole role, string? displayName)
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
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim();
            IsPlaceholder = false;
        }

        public static User CreatePlaceholder(Guid id, string displayName)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("User Id cannot be empty.", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be null or whitespace.", nameof(displayName));
            }

            return new User
            {
                Id = id,
                DisplayName = displayName.Trim(),
                Role = UserRole.User,
                IsPlaceholder = true
            };
        }

        public void Promote(string username, string email, string passwordHash)
        {
            if (!IsPlaceholder)
            {
                throw new InvalidOperationException($"User {Id} is not a placeholder.");
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

            Username = NormalizeUsername(username);
            Email = NormalizeEmail(email);
            PasswordHash = passwordHash;
            IsPlaceholder = false;
        }

        public void Rename(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be null or whitespace.", nameof(displayName));
            }

            DisplayName = displayName.Trim();
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
