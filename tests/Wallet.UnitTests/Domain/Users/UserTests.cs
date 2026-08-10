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
            var user = new User(id, "testuser", "hashedpassword", UserRole.User);
            user.Id.Should().Be(id);
            user.Username.Should().Be("testuser");
            user.PasswordHash.Should().Be("hashedpassword");
            user.Role.Should().Be(UserRole.User);
        }

        [Fact]
        public void Constructor_WithEmptyId_Throws()
        {
            var act = () => new User(Guid.Empty, "testuser", "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullUsername_Throws()
        {
            var act = () => new User(Guid.NewGuid(), null!, "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyUsername_Throws()
        {
            var act = () => new User(Guid.NewGuid(), string.Empty, "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullPasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", null!, UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyPasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", string.Empty, UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithWhitespaceUsername_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "   ", "hashedpassword", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithWhitespacePasswordHash_Throws()
        {
            var act = () => new User(Guid.NewGuid(), "testuser", "   ", UserRole.User);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NormalizesUsername()
        {
            var user = new User(Guid.NewGuid(), "  TestUser  ", "hashedpassword", UserRole.User);

            user.Username.Should().Be("testuser");
        }
    }
}
