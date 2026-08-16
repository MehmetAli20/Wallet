using FluentAssertions;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Common.Behaviors;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Idempotency;
using Wallet.IntegrationTests;

public class IdempotencyBehaviorTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    public IdempotencyBehaviorTests(PostgresFixture fixture) => _fixture = fixture;

    private record FakeRequest(string IdempotencyKey) : IIdempotentRequest;

    private static IdempotencyBehavior<FakeRequest, Guid> CreateBehavior(WalletDbContext context) =>
        new(new IdempotencyStore(context), new UnitOfWork(context));

    [Fact]
    public async Task FirstCall_InvokesHandler_AndStoresResponse()
    {
        var key = $"key-{Guid.NewGuid()}";
        var expected = Guid.NewGuid();
        var callCount = 0;

        await using var context = _fixture.CreateContext(TestCurrentUser.System);

        var result = await CreateBehavior(context).Handle(
            new FakeRequest(key),
            _ => { callCount++; return Task.FromResult(expected); },
            CancellationToken.None);

        result.Should().Be(expected);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task SecondCallWithSameKey_DoesNotInvokeHandler_AndReplaysResponse()
    {
        var key = $"key-{Guid.NewGuid()}";
        var expected = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            await CreateBehavior(context).Handle(
                new FakeRequest(key),
                _ => Task.FromResult(expected),
                CancellationToken.None);
        }

        var callCount = 0;

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var result = await CreateBehavior(context).Handle(
                new FakeRequest(key),
                _ => { callCount++; return Task.FromResult(Guid.NewGuid()); },
                CancellationToken.None);

            result.Should().Be(expected);
            callCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task RecordWithoutResponse_Throws()
    {
        var key = $"key-{Guid.NewGuid()}";

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            new IdempotencyStore(context).Stage(key, nameof(FakeRequest));
            await new UnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var act = async () => await CreateBehavior(context).Handle(
                new FakeRequest(key),
                _ => Task.FromResult(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<IdempotentResponseUnavailableException>();
        }
    }
}
