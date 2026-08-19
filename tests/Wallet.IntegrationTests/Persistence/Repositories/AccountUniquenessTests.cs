using FluentAssertions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Domain.Accounts;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.AccountRepository;
using Wallet.IntegrationTests;

namespace Wallet.IntegrationTests.Persistence.Repositories
{
    public class AccountUniquenessTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;
        public AccountUniquenessTests(PostgresFixture fixture) => _fixture = fixture;

        private async Task AddAsync(Guid ownerId, string currency)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            await new AccountRepository(context).AddAsync(new Account(Guid.NewGuid(), ownerId, currency));
            await new UnitOfWork(context).SaveChangesAsync();
        }

        [Fact]
        public async Task SameOwner_SameCurrency_IsRejected()
        {
            var ownerId = Guid.NewGuid();
            await AddAsync(ownerId, "USD");

            var act = async () => await AddAsync(ownerId, "USD");

            await act.Should().ThrowAsync<UniqueConstraintViolationException>();
        }

        [Fact]
        public async Task SameOwner_DifferentCurrency_IsAllowed()
        {
            var ownerId = Guid.NewGuid();

            await AddAsync(ownerId, "USD");
            var act = async () => await AddAsync(ownerId, "EUR");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DifferentOwners_SameCurrency_IsAllowed()
        {
            await AddAsync(Guid.NewGuid(), "USD");
            var act = async () => await AddAsync(Guid.NewGuid(), "USD");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task CurrencyCasing_DoesNotBypassUniqueness()
        {
            var ownerId = Guid.NewGuid();
            await AddAsync(ownerId, "USD");

            var act = async () => await AddAsync(ownerId, "usd");

            await act.Should().ThrowAsync<UniqueConstraintViolationException>();
        }
    }
}
