using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Domain.Users;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.Users;
using Wallet.IntegrationTests;

public class UserRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    public UserRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetByEmail_IgnoresSurroundingWhitespace()
    {
        var id = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var repo = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new User(id, "mehmet@test.com", "hash", UserRole.User, "Test User"));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var repo = new UserRepository(context);
            var found = await repo.GetByEmailAsync("  MeHmEt@Test.COM  ");

            found.Should().NotBeNull();
            found!.Id.Should().Be(id);
        }
    }

    [Fact]
    public async Task GetByEmail_FindsUser_RegardlessOfCasing()
    {
        var id = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var repo = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new User(id, "  Elif@Example.COM  ", "hash", UserRole.User, "Test User"));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var repo = new UserRepository(context);
            var found = await repo.GetByEmailAsync("ELIF@EXAMPLE.COM");

            found.Should().NotBeNull();
            found!.Id.Should().Be(id);
            found.Email.Should().Be("elif@example.com");
        }
    }

    [Fact]
    public async Task AddingEmailThatDiffersOnlyByCasing_IsRejected()
    {
        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var repo = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new User(Guid.NewGuid(), "Burak@Example.com", "hash", UserRole.User, "Test User"));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var repo = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new User(Guid.NewGuid(), "BURAK@example.COM", "hash", UserRole.User, "Test User"));

            var act = async () => await unitOfWork.SaveChangesAsync();

            await act.Should().ThrowAsync<UniqueConstraintViolationException>();
        }
    }
}