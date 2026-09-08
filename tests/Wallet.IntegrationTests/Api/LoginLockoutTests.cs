using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Users;
using Wallet.Domain.Users;

namespace Wallet.IntegrationTests.Api
{
    public class LoginLockoutTests : IClassFixture<ApiFixture>
    {
        private const string Password = "password123";

        private readonly ApiFixture _fixture;

        public LoginLockoutTests(ApiFixture fixture) => _fixture = fixture;

        private async Task<(HttpClient Client, string Username)> ARegisteredUserAsync()
        {
            var client = _fixture.CreateClient();
            var username = $"u{Guid.NewGuid():N}"[..20];

            var register = await client.PostAsJsonAsync("/api/v1/auth/register",
                new RegisterRequest(username, $"{username}@test.com", Password));
            register.StatusCode.Should().Be(HttpStatusCode.Created);

            return (client, username);
        }

        private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password) =>
            client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));

        [Fact]
        public async Task AfterTheThreshold_EvenTheCorrectPasswordIsRejected()
        {
            var (client, username) = await ARegisteredUserAsync();

            for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
            {
                var failed = await LoginAsync(client, username, "wrong-password");

                failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }

            var locked = await LoginAsync(client, username, Password);

            locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ASuccessfulLogin_ClearsTheFailuresBehindIt()
        {
            var (client, username) = await ARegisteredUserAsync();

            for (var i = 0; i < User.MaxFailedAccessAttempts - 1; i++)
            {
                await LoginAsync(client, username, "wrong-password");
            }

            var recovered = await LoginAsync(client, username, Password);
            recovered.StatusCode.Should().Be(HttpStatusCode.OK);

            for (var i = 0; i < User.MaxFailedAccessAttempts - 1; i++)
            {
                await LoginAsync(client, username, "wrong-password");
            }

            var stillOpen = await LoginAsync(client, username, Password);

            stillOpen.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ALockedAccount_IsIndistinguishableFromAnUnknownOne()
        {
            var (client, username) = await ARegisteredUserAsync();

            for (var i = 0; i < User.MaxFailedAccessAttempts; i++)
            {
                await LoginAsync(client, username, "wrong-password");
            }

            var locked = await LoginAsync(client, username, Password);
            var unknown = await LoginAsync(client, $"u{Guid.NewGuid():N}"[..20], Password);

            locked.StatusCode.Should().Be(unknown.StatusCode);
            (await locked.Content.ReadAsStringAsync())
                .Should().Be(await unknown.Content.ReadAsStringAsync());
        }
    }
}
