using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Users;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.UserRepository;
using Wallet.IntegrationTests;

public class UserRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    public UserRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetByUsername_FindsUser_RegardlessOfCasing()
    {
        var id = Guid.NewGuid();

        await using (var context = _fixture.CreateContext())
        {
            var repo = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new User(id, "Ahmet", "test0@test.com", "hash", UserRole.User));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repo = new UserRepository(context);
            var found = await repo.GetByUsernameAsync("AHMET");

            found.Should().NotBeNull();
            found!.Id.Should().Be(id);
            found.Username.Should().Be("ahmet");
            found.Email.Should().Be("test0@test.com");
        }
    }

    [Fact]
    public async Task GetByUsername_IgnoresSurroundingWhitespace()
    {
        var id = Guid.NewGuid();

        await using (var context = _fixture.CreateContext())
        {
            var repo = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new User(id, "Mehmet", "test1@test.com", "hash", UserRole.User));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repo = new UserRepository(context);
            var found = await repo.GetByUsernameAsync("  MeHmEt  ");

            found.Should().NotBeNull();
            found!.Id.Should().Be(id);
        }
    }

    [Fact]
    public async Task AddingUsernameThatDiffersOnlyByCasing_IsRejected()
    {
        await using (var context = _fixture.CreateContext())
        {
            var repo = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new User(Guid.NewGuid(), "Zeynep", "test2@test.com", "hash", UserRole.User));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repo = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new User(Guid.NewGuid(), "zeynep", "test2@test.com", "hash", UserRole.User));

            var act = async () => await unitOfWork.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }
}