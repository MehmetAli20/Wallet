using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Users;
using Wallet.Domain.Users;

namespace Wallet.IntegrationTests.Api
{
    public class LoginLockoutTests : IClassFixture<ClientIpApiFixture>
    {
        private const string Password = "password123";
        private const string HomeIp = "203.0.113.10";
        private const string AttackerIp = "198.51.100.20";
        private const string AnotherStrangerIp = "192.0.2.30";

        private readonly ClientIpApiFixture _fixture;

        public LoginLockoutTests(ClientIpApiFixture fixture) => _fixture = fixture;

        private async Task<(HttpClient Client, string Email)> ARegisteredUserAsync()
        {
            var client = _fixture.CreateClient();
            var email = $"u{Guid.NewGuid():N}@test.com";

            var register = await client.PostAsJsonAsync("/api/v1/auth/register",
                new RegisterRequest(email, Password, ApiFixture.DisplayName));
            register.StatusCode.Should().Be(HttpStatusCode.Created);

            return (client, email);
        }

        private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
            client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));

        private Task<HttpResponseMessage> LoginFromAsync(string clientIp, string email, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest(email, password))
            };
            request.Headers.Add(ClientIpApiFixture.ClientIpHeader, clientIp);

            return _fixture.CreateClient().SendAsync(request);
        }

        [Fact]
        public async Task AfterTheThreshold_EvenTheCorrectPasswordIsRejected()
        {
            var (client, email) = await ARegisteredUserAsync();

            for (var i = 0; i < LoginLockout.MaxFailedAttempts; i++)
            {
                var failed = await LoginAsync(client, email,"wrong-password");

                failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }

            var locked = await LoginAsync(client, email,Password);

            locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ASuccessfulLogin_ClearsTheFailuresBehindIt()
        {
            var (client, email) = await ARegisteredUserAsync();

            for (var i = 0; i < LoginLockout.MaxFailedAttempts - 1; i++)
            {
                await LoginAsync(client, email,"wrong-password");
            }

            var recovered = await LoginAsync(client, email,Password);
            recovered.StatusCode.Should().Be(HttpStatusCode.OK);

            for (var i = 0; i < LoginLockout.MaxFailedAttempts - 1; i++)
            {
                await LoginAsync(client, email,"wrong-password");
            }

            var stillOpen = await LoginAsync(client, email,Password);

            stillOpen.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task FailuresSentInParallel_AreAllCounted()
        {
            var (client, email) = await ARegisteredUserAsync();

            var burst = Enumerable
                .Range(0, 20)
                .Select(_ => LoginAsync(client, email,"wrong-password"));

            await Task.WhenAll(burst);

            var crossesTheThreshold = await LoginAsync(client, email,"wrong-password");
            crossesTheThreshold.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var locked = await LoginAsync(client, email,Password);

            locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ALockedAccount_IsIndistinguishableFromAnUnknownOne()
        {
            var (client, email) = await ARegisteredUserAsync();

            for (var i = 0; i < LoginLockout.MaxFailedAttempts; i++)
            {
                await LoginAsync(client, email,"wrong-password");
            }

            var locked = await LoginAsync(client, email,Password);
            var unknown = await LoginAsync(client, $"u{Guid.NewGuid():N}@test.com", Password);

            locked.StatusCode.Should().Be(unknown.StatusCode);
            (await locked.Content.ReadAsStringAsync())
                .Should().Be(await unknown.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FailuresFromStrangers_DoNotLockTheOwnerOutOfAFamiliarNetwork()
        {
            var (_, email) = await ARegisteredUserAsync();
            (await LoginFromAsync(HomeIp, email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);

            for (var i = 0; i < LoginLockout.MaxFailedAttempts; i++)
                await LoginFromAsync(AttackerIp, email, "wrong-password");

            (await LoginFromAsync(AttackerIp, email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await LoginFromAsync(AnotherStrangerIp, email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await LoginFromAsync(HomeIp, email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task FailuresFromAFamiliarNetwork_LockOnlyThatLane()
        {
            var (_, email) = await ARegisteredUserAsync();
            (await LoginFromAsync(HomeIp, email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);

            for (var i = 0; i < LoginLockout.MaxFailedAttempts; i++)
                await LoginFromAsync(HomeIp, email, "wrong-password");

            (await LoginFromAsync(HomeIp, email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await LoginFromAsync(AttackerIp, email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
