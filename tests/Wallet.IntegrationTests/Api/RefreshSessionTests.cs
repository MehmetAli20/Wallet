using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wallet.Api.Contracts.Users;
using Wallet.Domain.Users;

namespace Wallet.IntegrationTests.Api
{
    public class RefreshSessionTests : IClassFixture<ApiFixture>
    {
        private const string CookieName = "wallet_refresh";

        private readonly ApiFixture _fixture;

        public RefreshSessionTests(ApiFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task Login_SetsAHardenedRefreshCookie()
        {
            var (_, login) = await SignInAsync();

            var header = RefreshSetCookieHeader(login).ToLowerInvariant();

            header.Should().Contain("httponly");
            header.Should().Contain("secure");
            header.Should().Contain("samesite=strict");
            header.Should().Contain("path=/api/v1/auth");
        }

        [Fact]
        public async Task Refresh_GivesAWorkingAccessToken_AndRotatesTheCookie()
        {
            var (refreshToken, _) = await SignInAsync();

            var refreshed = await RefreshAsync(refreshToken);
            refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

            RefreshCookieFrom(refreshed).Should().NotBe(refreshToken);

            var accessToken = (await refreshed.Content.ReadFromJsonAsync<LoginResponse>())!.Token;

            var client = NewClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            (await client.GetAsync("/api/v1/groups")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Refresh_WithoutACookie_Returns401()
        {
            (await RefreshAsync(null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WithAnUnknownToken_Returns401()
        {
            var unknown = $"{RefreshToken.TokenPrefix}_{new string('a', 43)}";

            (await RefreshAsync(unknown)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReplayingRightAway_IsTolerated()
        {
            var (first, _) = await SignInAsync();

            (await RefreshAsync(first)).StatusCode.Should().Be(HttpStatusCode.OK);

            var replay = await RefreshAsync(first);

            replay.StatusCode.Should().Be(HttpStatusCode.OK);
            (await RefreshAsync(RefreshCookieFrom(replay))).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ReplayingAfterTheToleranceWindow_RevokesTheWholeSession()
        {
            var (first, _) = await SignInAsync();

            var rotated = await RefreshAsync(first);
            rotated.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = RefreshCookieFrom(rotated);

            await Task.Delay(RefreshToken.ReuseInterval + TimeSpan.FromSeconds(1));

            (await RefreshAsync(first)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await RefreshAsync(second)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_EndsTheSession_AndClearsTheCookie()
        {
            var (refreshToken, _) = await SignInAsync();

            var logout = await LogoutAsync(refreshToken);

            logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
            logout.Headers.GetValues("Set-Cookie")
                .Should().Contain(h => h.StartsWith($"{CookieName}=;", StringComparison.Ordinal));

            (await RefreshAsync(refreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_WithoutACookie_StillSucceeds()
        {
            (await LogoutAsync(null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private HttpClient NewClient() =>
            _fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        private async Task<(string RefreshToken, HttpResponseMessage Login)> SignInAsync()
        {
            var client = NewClient();
            var username = $"u{Guid.NewGuid():N}"[..20];

            var register = await client.PostAsJsonAsync("/api/v1/auth/register",
                new RegisterRequest(username, $"{username}@test.com", "password123"));
            register.StatusCode.Should().Be(HttpStatusCode.Created);

            var login = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(username, "password123"));
            login.StatusCode.Should().Be(HttpStatusCode.OK);

            return (RefreshCookieFrom(login), login);
        }

        private static string RefreshSetCookieHeader(HttpResponseMessage response) =>
            response.Headers.GetValues("Set-Cookie")
                .Single(h => h.StartsWith($"{CookieName}=", StringComparison.Ordinal));

        private static string RefreshCookieFrom(HttpResponseMessage response) =>
            RefreshSetCookieHeader(response).Split(';')[0][(CookieName.Length + 1)..];

        private async Task<HttpResponseMessage> PostWithRefreshCookieAsync(string path, string? refreshToken)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, path);

            if (refreshToken is not null)
                message.Headers.Add("Cookie", $"{CookieName}={refreshToken}");

            return await NewClient().SendAsync(message);
        }

        private Task<HttpResponseMessage> RefreshAsync(string? refreshToken) =>
            PostWithRefreshCookieAsync("/api/v1/auth/refresh", refreshToken);

        private Task<HttpResponseMessage> LogoutAsync(string? refreshToken) =>
            PostWithRefreshCookieAsync("/api/v1/auth/logout", refreshToken);
    }
}
