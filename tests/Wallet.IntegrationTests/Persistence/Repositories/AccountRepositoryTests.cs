using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories;
using Wallet.IntegrationTests;

public class AccountRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    public AccountRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Account_WithLedgerEntries_RoundTripsThroughDatabase()
    {
        var id = Guid.NewGuid();

        // YAZ — kendi context'inde
        await using (var context = _fixture.CreateContext())
        {
            var repo = new AccountRepository(context);
            var unitOfWork = new UnitOfWork(context);
            var account = new Account(id, new Money(100m, "USD"));
            account.Deposit(new Money(50m, "USD"));
            account.Withdraw(new Money(20m, "USD"));

            await repo.AddAsync(account);
            await unitOfWork.SaveChangesAsync();
        }

        // OKU — TAMAMEN YENİ context'te
        await using (var context = _fixture.CreateContext())
        {
            var repo = new AccountRepository(context);
            var loaded = await repo.GetByIdAsync(id);

            loaded.Should().NotBeNull();
            loaded!.Balance.Should().Be(new Money(130m, "USD"));
            loaded.Entries.Should().HaveCount(2);
            loaded.Entries[0].Type.Should().Be(LedgerEntryType.Credit);
            loaded.Entries[1].Type.Should().Be(LedgerEntryType.Debit);
        }
    }
}