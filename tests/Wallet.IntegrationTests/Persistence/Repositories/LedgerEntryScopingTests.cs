using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.AccountRepository;

namespace Wallet.IntegrationTests.Persistence
{
    public class LedgerEntryScopingTests : IClassFixture<PostgresFixture>
    {
        private static readonly Guid GroupId = Guid.NewGuid();
        private static readonly Guid Counterparty = Guid.NewGuid();

        private readonly PostgresFixture _fixture;
        public LedgerEntryScopingTests(PostgresFixture fixture) => _fixture = fixture;

        private async Task SeedAsync(Guid ownerId)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var account = new Account(Guid.NewGuid(), ownerId, "USD");
            account.Credit(new Money(50m, "USD"), GroupId, Counterparty);

            await new AccountRepository(context).AddAsync(account);
            await new UnitOfWork(context).SaveChangesAsync();
        }

        [Fact]
        public async Task User_SeesOnlyOwnEntries()
        {
            var ownerA = Guid.NewGuid();
            var ownerB = Guid.NewGuid();
            await SeedAsync(ownerA);
            await SeedAsync(ownerB);

            await using var context = _fixture.CreateContext(TestCurrentUser.For(ownerA));
            var entries = await context.Set<LedgerEntry>().ToListAsync();

            entries.Should().NotBeEmpty();
            entries.Should().OnlyContain(e => e.OwnerId == ownerA);
        }

        [Fact]
        public async Task SystemContext_SeesAllEntries()
        {
            var ownerA = Guid.NewGuid();
            var ownerB = Guid.NewGuid();
            await SeedAsync(ownerA);
            await SeedAsync(ownerB);

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var entries = await context.Set<LedgerEntry>().ToListAsync();

            entries.Should().Contain(e => e.OwnerId == ownerA);
            entries.Should().Contain(e => e.OwnerId == ownerB);
        }
    }
}