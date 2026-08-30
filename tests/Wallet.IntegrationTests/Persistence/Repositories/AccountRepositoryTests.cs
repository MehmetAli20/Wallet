using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.Accounts;
using Wallet.IntegrationTests;

public class AccountRepositoryTests : IClassFixture<PostgresFixture>
{
        private static readonly Guid GroupId = Guid.NewGuid();
        private static readonly Guid Counterparty = Guid.NewGuid();

    private readonly PostgresFixture _fixture;
    public AccountRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Account_WithLedgerEntries_RoundTripsThroughDatabase()
    {
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(TestCurrentUser.For(ownerId)))
        {
            var repo = new AccountRepository(context);
            var unitOfWork = new UnitOfWork(context);
            var account = new Account(id, ownerId, "USD");
            account.Credit(new Money(50m, "USD"), GroupId, Counterparty);
            account.Debit(new Money(20m, "USD"), GroupId, Counterparty);

            await repo.AddAsync(account);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.For(ownerId)))
        {
            var repo = new AccountRepository(context);
            var loaded = await repo.GetByIdAsync(id);

            loaded.Should().NotBeNull();
            loaded!.Balance.Should().Be(new Money(30m, "USD"));

            var entries = await repo.GetEntriesAsync(id, skip: 0, take: 50);

            entries.Should().HaveCount(2);
            entries[0].Type.Should().Be(LedgerEntryType.Debit);
            entries[1].Type.Should().Be(LedgerEntryType.Credit);
        }
    }

    [Fact]
    public async Task GetById_ReturnsNull_ForAnotherUser()
    {
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(TestCurrentUser.For(ownerId)))
        {
            var repo = new AccountRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new Account(id, ownerId, "USD"));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.For(otherUserId)))
        {
            var repo = new AccountRepository(context);
            var loaded = await repo.GetByIdAsync(id);

            loaded.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetById_ReturnsAccount_ForSystemContext()
    {
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(TestCurrentUser.For(ownerId)))
        {
            var repo = new AccountRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new Account(id, ownerId, "USD"));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var repo = new AccountRepository(context);
            var loaded = await repo.GetByIdAsync(id);

            loaded.Should().NotBeNull();
            loaded!.OwnerId.Should().Be(ownerId);
        }
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentUsersAccounts()
    {
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(TestCurrentUser.For(ownerId)))
        {
            var repo = new AccountRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new Account(Guid.NewGuid(), ownerId, "USD"));
            await repo.AddAsync(new Account(Guid.NewGuid(), ownerId, "EUR"));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.For(otherOwnerId)))
        {
            var repo = new AccountRepository(context);
            var unitOfWork = new UnitOfWork(context);

            await repo.AddAsync(new Account(Guid.NewGuid(), otherOwnerId, "USD"));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.For(ownerId)))
        {
            var accounts = await new AccountRepository(context).GetAllAsync();

            accounts.Should().HaveCount(2);
            accounts.Should().OnlyContain(a => a.OwnerId == ownerId);
        }
    }
}