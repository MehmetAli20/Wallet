using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Users;

namespace Wallet.IntegrationTests.RateLimiting
{
    public class WriteRateLimitTests : IClassFixture<WriteLimitFixture>
    {
        private readonly WriteLimitFixture _fixture;

        public WriteRateLimitTests(WriteLimitFixture fixture) => _fixture = fixture;

        private static Task<HttpResponseMessage> CreateGroup(HttpClient client) =>
            client.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest("Piknik", "TRY"));

        [Fact]
        public async Task AWriteBurst_StopsAtTheWriteCapacity()
        {
            var client = await _fixture.RegisterAsync();

            for (var i = 0; i < 3; i++)
            {
                (await CreateGroup(client)).StatusCode.Should().Be(HttpStatusCode.Created);
            }

            var denied = await CreateGroup(client);

            denied.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            denied.Headers.RetryAfter.Should().NotBeNull();
            denied.Headers.GetValues("X-RateLimit-Remaining").Single().Should().Be("0");
        }

        [Fact]
        public async Task ReadsSurvive_AnExhaustedWriteBucket()
        {
            var client = await _fixture.RegisterAsync();

            for (var i = 0; i < 4; i++)
            {
                await CreateGroup(client);
            }

            var read = await client.GetAsync("/api/v1/groups");

            read.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task TwoUsers_DoNotShareABucket()
        {
            var alice = await _fixture.RegisterAsync();
            var bob = await _fixture.RegisterAsync();

            for (var i = 0; i < 4; i++)
            {
                await CreateGroup(alice);
            }

            (await CreateGroup(alice)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            (await CreateGroup(bob)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task AnAllowedWrite_CarriesTheLimitHeaders()
        {
            var client = await _fixture.RegisterAsync();

            var allowed = await CreateGroup(client);

            allowed.Headers.GetValues("X-RateLimit-Limit").Single().Should().Be("3");
            allowed.Headers.GetValues("X-RateLimit-Remaining").Single().Should().Be("2");
        }
    }

    public class AnonymousRateLimitTests : IClassFixture<AnonymousLimitFixture>
    {
        private readonly AnonymousLimitFixture _fixture;

        public AnonymousRateLimitTests(AnonymousLimitFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task AnonymousCallers_ShareOneBucketPerAddress()
        {
            var client = _fixture.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                var attempt = await client.PostAsJsonAsync("/api/v1/auth/login",
                    new LoginRequest("nobody", "wrong-password"));

                attempt.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
            }

            var denied = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest("nobody", "wrong-password"));

            denied.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            denied.Headers.RetryAfter.Should().NotBeNull();
        }
    }
}
