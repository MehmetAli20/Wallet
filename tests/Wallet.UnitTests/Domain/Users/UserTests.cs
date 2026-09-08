using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Users;

namespace Wallet.UnitTests.Domain.Users
{
    public class UserTests
    {
        [Fact]
        public void Constructor_WithValidInput_SetsProperties()
        {
            var id = Guid.NewGuid();
            var user = new User(id, "testuser","test@test.com", "hashedpassword", UserRole.User);
            user.Id.Should().Be(id);
            user.Username.Should().Be("testuser");
            user.Email.Should().Be("test@test.com");
            user.PasswordHash.Should().Be("hashedpassword");
            user.Role.Should().Be(UserRole.User);
        }

        [Fact]
        public void Constructor_WithEmptyId_Throws()
        {
            var act = () => new User(Guid.Empty, "testuser", "test@test.com", "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullUsername_Throws()
        {
            var act = () => new User(Guid.NewGuid(), null!, "test@test.com", "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyUsername_Throws()
        {
            var act = () => new User(Guid.NewGuid(), string.Empty, "test@test.com", "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullPasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", "test@test.com", null!, UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyPasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", "test@test.com", string.Empty, UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithWhitespaceUsername_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "   ", "test@test.com", "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithWhitespacePasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", "test@test.com", "   ", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullEmail_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", null!, "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyEmail_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", string.Empty, "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithWhitespaceEmail_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", "   ", "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NormalizesUsername()
        {
            var user = new User(Guid.NewGuid(), "  TestUser  ", "test@test.com", "hashedpassword", UserRole.User);

            user.Username.Should().Be("testuser");
        }

        [Fact]
        public void Constructor_NormalizesEmail()
        {
            var user = new User(Guid.NewGuid(), "testuser", "  Test@Example.COM  ", "hashedpassword", UserRole.User);

            user.Email.Should().Be("test@example.com");
        }

        private static User ASignedUpUser() =>
            new(Guid.NewGuid(), "testuser", "test@test.com", "hashedpassword", UserRole.User);

        [Fact]
        public void ANewUser_IsNotLockedOut()
        {
            var user = ASignedUpUser();

            user.IsLockedOut(DateTimeOffset.UtcNow).Should().BeFalse();
            user.AccessFailedCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public void RegisterFailedAccess_BelowTheThreshold_DoesNotLock()
        {
            var user = ASignedUpUser();
            var now = DateTimeOffset.UtcNow;

            for (var i = 0; i < User.MaxFailedAccessAttempts - 1; i++)
            {
                user.RegisterFailedAccess(now);
            }

            user.IsLockedOut(now).Should().BeFalse();
            user.AccessFailedCount.Should().Be(User.MaxFailedAccessAttempts - 1);
        }

        [Fact]
        public void RegisterFailedAccess_AtTheThreshold_LocksAndClearsTheCounter()
        {
            var user = ASignedUpUser();
            var now = DateTimeOffset.UtcNow;

            for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
            {
                user.RegisterFailedAccess(now);
            }

            user.IsLockedOut(now).Should().BeTrue();
            user.AccessFailedCount.Should().Be(0);
            user.LockoutEnd.Should().BeCloseTo(now.Add(User.LockoutDuration), TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void ALockedUser_IsOpenAgain_OnceTheWindowPasses()
        {
            var user = ASignedUpUser();
            var now = DateTimeOffset.UtcNow;

            for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
            {
                user.RegisterFailedAccess(now);
            }

            user.IsLockedOut(now.Add(User.LockoutDuration).AddSeconds(-1)).Should().BeTrue();
            user.IsLockedOut(now.Add(User.LockoutDuration).AddSeconds(1)).Should().BeFalse();
        }

        [Fact]
        public void ResetAccessFailures_ClearsBothTheCounterAndTheLock()
        {
            var user = ASignedUpUser();
            var now = DateTimeOffset.UtcNow;

            for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
            {
                user.RegisterFailedAccess(now);
            }

            user.ResetAccessFailures();

            user.IsLockedOut(now).Should().BeFalse();
            user.AccessFailedCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public void ASuccessInBetween_MeansTheFailuresNeverAddUp()
        {
            var user = ASignedUpUser();
            var now = DateTimeOffset.UtcNow;

            for (var i = 0; i < User.MaxFailedAccessAttempts - 1; i++)
            {
                user.RegisterFailedAccess(now);
            }

            user.ResetAccessFailures();

            for (var i = 0; i < User.MaxFailedAccessAttempts - 1; i++)
            {
                user.RegisterFailedAccess(now);
            }

            user.IsLockedOut(now).Should().BeFalse();
        }

        [Fact]
        public void LockoutEnd_IsStoredInUtc()
        {
            var user = ASignedUpUser();
            var local = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.FromHours(3));

            for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
            {
                user.RegisterFailedAccess(local);
            }

            user.LockoutEnd!.Value.Offset.Should().Be(TimeSpan.Zero);
        }
    }
}