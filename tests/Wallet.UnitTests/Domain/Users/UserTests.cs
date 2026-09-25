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
            var user = new User(id, "test@test.com", "hashedpassword", UserRole.User, "Test User");
            user.Id.Should().Be(id);
            user.DisplayName.Should().Be("Test User");
            user.Email.Should().Be("test@test.com");
            user.PasswordHash.Should().Be("hashedpassword");
            user.Role.Should().Be(UserRole.User);
        }

        [Fact]
        public void Constructor_WithEmptyId_Throws()
        {
            var act = () => new User(Guid.Empty, "test@test.com", "hashedpassword", UserRole.User, "Test User");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullPasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(),"test@test.com", null!, UserRole.User, "Test User");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyPasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(),"test@test.com", string.Empty, UserRole.User, "Test User");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithWhitespacePasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(),"test@test.com", "   ", UserRole.User, "Test User");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullEmail_Throws()
        {
            var act = () => new User(Guid.NewGuid(),null!, "hashedpassword", UserRole.User, "Test User");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyEmail_Throws()
        {
            var act = () => new User(Guid.NewGuid(),string.Empty, "hashedpassword", UserRole.User, "Test User");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithWhitespaceEmail_Throws()
        {
            var act = () => new User(Guid.NewGuid(),"   ", "hashedpassword", UserRole.User, "Test User");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NormalizesEmail()
        {
            var user = new User(Guid.NewGuid(),"  Test@Example.COM  ", "hashedpassword", UserRole.User, "Test User");

            user.Email.Should().Be("test@example.com");
        }
    }
}
