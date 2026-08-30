using FluentAssertions;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Common.Behaviors;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Idempotency;
using Wallet.IntegrationTests;

public class IdempotencyBehaviorTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    public IdempotencyBehaviorTests(PostgresFixture fixture) => _fixture = fixture;

    private record FakeRequest(string IdempotencyKey, decimal Amount = 0m) : IIdempotentRequest;

    private record OtherFakeRequest(string IdempotencyKey) : IIdempotentRequest;

    private static IdempotencyBehavior<FakeRequest, Guid> CreateBehavior(
        WalletDbContext context, ICurrentUser currentUser) =>
        new(new IdempotencyStore(context, currentUser), new UnitOfWork(context));

    [Fact]
    public async Task FirstCall_InvokesHandler_AndStoresResponse()
    {
        var key = $"key-{Guid.NewGuid()}";
        var expected = Guid.NewGuid();
        var callCount = 0;

        await using var context = _fixture.CreateContext(TestCurrentUser.System);

        var result = await CreateBehavior(context, TestCurrentUser.System).Handle(
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
            await CreateBehavior(context, TestCurrentUser.System).Handle(
                new FakeRequest(key),
                _ => Task.FromResult(expected),
                CancellationToken.None);
        }

        var callCount = 0;

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var result = await CreateBehavior(context, TestCurrentUser.System).Handle(
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
            new IdempotencyStore(context, TestCurrentUser.System)
                .Stage(key, nameof(FakeRequest), requestHash: null);
            await new UnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext(TestCurrentUser.System))
        {
            var act = async () => await CreateBehavior(context, TestCurrentUser.System).Handle(
                new FakeRequest(key),
                _ => Task.FromResult(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<IdempotentResponseUnavailableException>();
        }
    }

    [Fact]
    public async Task TheSameKeyFromAnotherUser_RunsItsOwnHandler_AndNeverSeesTheFirstResponse()
    {
        var key = $"key-{Guid.NewGuid()}";
        var alice = TestCurrentUser.For(Guid.NewGuid());
        var bob = TestCurrentUser.For(Guid.NewGuid());

        var aliceResult = Guid.NewGuid();
        var bobResult = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(alice))
        {
            await CreateBehavior(context, alice).Handle(
                new FakeRequest(key),
                _ => Task.FromResult(aliceResult),
                CancellationToken.None);
        }

        var callCount = 0;

        await using (var context = _fixture.CreateContext(bob))
        {
            var result = await CreateBehavior(context, bob).Handle(
                new FakeRequest(key),
                _ => { callCount++; return Task.FromResult(bobResult); },
                CancellationToken.None);

            result.Should().Be(bobResult);
            result.Should().NotBe(aliceResult);
            callCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task TheSameKeyForADifferentCommand_IsRejected()
    {
        var key = $"key-{Guid.NewGuid()}";
        var user = TestCurrentUser.For(Guid.NewGuid());

        await using (var context = _fixture.CreateContext(user))
        {
            await CreateBehavior(context, user).Handle(
                new FakeRequest(key),
                _ => Task.FromResult(Guid.NewGuid()),
                CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext(user))
        {
            var behavior = new IdempotencyBehavior<OtherFakeRequest, Guid>(
                new IdempotencyStore(context, user), new UnitOfWork(context));

            var act = async () => await behavior.Handle(
                new OtherFakeRequest(key),
                _ => Task.FromResult(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<IdempotencyKeyReuseException>();
        }
    }

    [Fact]
    public async Task TheSameKeyWithADifferentBody_IsRejected()
    {
        var key = $"key-{Guid.NewGuid()}";
        var user = TestCurrentUser.For(Guid.NewGuid());

        await using (var context = _fixture.CreateContext(user))
        {
            await CreateBehavior(context, user).Handle(
                new FakeRequest(key, 100m),
                _ => Task.FromResult(Guid.NewGuid()),
                CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext(user))
        {
            var act = async () => await CreateBehavior(context, user).Handle(
                new FakeRequest(key, 250m),
                _ => Task.FromResult(Guid.NewGuid()),
                CancellationToken.None);

            await act.Should().ThrowAsync<IdempotencyPayloadMismatchException>();
        }
    }

    [Fact]
    public async Task TheSameKeyWithTheSameBody_StillReplays()
    {
        var key = $"key-{Guid.NewGuid()}";
        var user = TestCurrentUser.For(Guid.NewGuid());
        var expected = Guid.NewGuid();

        await using (var context = _fixture.CreateContext(user))
        {
            await CreateBehavior(context, user).Handle(
                new FakeRequest(key, 100m),
                _ => Task.FromResult(expected),
                CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext(user))
        {
            var result = await CreateBehavior(context, user).Handle(
                new FakeRequest(key, 100m),
                _ => Task.FromResult(Guid.NewGuid()),
                CancellationToken.None);

            result.Should().Be(expected);
        }
    }
}
