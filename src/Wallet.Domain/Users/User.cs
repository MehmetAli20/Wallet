using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Wallet.Domain.Users
{
    public class User
    {
        public const int DisplayNameMaxLength = 100;

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

        public User(Guid id, string username, string email, string passwordHash, UserRole role, string displayName)
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
            DisplayName = NormalizeDisplayName(displayName);
            IsPlaceholder = false;
        }

        public static User CreatePlaceholder(Guid id, string displayName)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("User Id cannot be empty.", nameof(id));
            }

            return new User
            {
                Id = id,
                DisplayName = NormalizeDisplayName(displayName),
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
            DisplayName = NormalizeDisplayName(displayName);
        }

        public static string NormalizeUsername(string username)
        {
            return username.Trim().ToLowerInvariant();
        }

        public static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        public static string NormalizeDisplayName(string displayName)
        {
            if (!TryNormalizeDisplayName(displayName, out var normalized))
            {
                throw new ArgumentException(
                    "Display name is empty, too long or contains characters that are not allowed.", nameof(displayName));
            }

            return normalized;
        }

        public static string DisplayNameKey(string displayName)
        {
            return NormalizeDisplayName(displayName).ToLowerInvariant();
        }

        public static bool TryNormalizeDisplayName(string? displayName, out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return false;
            }

            string composed;

            try
            {
                composed = displayName.Normalize(NormalizationForm.FormKC);
            }
            catch (ArgumentException)
            {
                return false;
            }

            var builder = new StringBuilder(composed.Length);
            var pendingSpace = false;

            foreach (var rune in composed.EnumerateRunes())
            {
                var category = Rune.GetUnicodeCategory(rune);

                if (category is UnicodeCategory.Control
                    or UnicodeCategory.Format
                    or UnicodeCategory.PrivateUse
                    or UnicodeCategory.OtherNotAssigned)
                {
                    return false;
                }

                if (Rune.IsWhiteSpace(rune))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(rune.ToString());
            }

            if (builder.Length is 0 or > DisplayNameMaxLength)
            {
                return false;
            }

            normalized = builder.ToString();
            return true;
        }
    }
}
