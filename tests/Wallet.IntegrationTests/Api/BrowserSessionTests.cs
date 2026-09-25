using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Api.Contracts.Users;
using Wallet.Domain.Users;

namespace Wallet.IntegrationTests.Api
{
    public class BrowserSessionTests : IClassFixture<ApiFixture>
    {
        private const string CookieName = "__Host-wallet_session";
        private const string CsrfHeader = "X-CSRF";
        private const string Password = "password123";
        private const string SessionPath = "/api/v1/auth/session";
        private const string RefreshPath = "/api/v1/auth/session/refresh";
        private const string GroupsPath = "/api/v1/groups";

        private static readonly string UnknownSession = $"{RefreshToken.TokenPrefix}_{new string('a', 43)}";

        private readonly ApiFixture _fixture;

        public BrowserSessionTests(ApiFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task StartingASession_SetsAHardenedHostCookie_AndReturnsNoToken()
        {
            var started = await StartSessionAsync(await RegisterAsync());

            started.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await started.Content.ReadAsStringAsync()).Should().BeEmpty();

            var attributes = CookieAttributes(SessionSetCookieHeader(started));

            attributes.Should().Contain(new[] { "httponly", "secure", "samesite=strict", "path=/" });
            attributes.Should().NotContain(a => a.StartsWith("domain=", StringComparison.Ordinal));
        }

        [Fact]
        public async Task BearerLogin_OpensNoBrowserSession()
        {
            var username = await RegisterAsync();

            var login = await NewClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, Password));

            login.StatusCode.Should().Be(HttpStatusCode.OK);
            (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token.Should().NotBeNullOrEmpty();
            login.Headers.Contains("Set-Cookie").Should().BeFalse();
        }

        [Fact]
        public async Task AnUnknownSessionCookie_IsNotAuthenticated()
        {
            (await ListGroupsAsync(UnknownSession)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_RotatesTheCookie_AndTheNewOneAuthenticates()
        {
            var first = await SignInAsync();

            var refreshed = await RefreshAsync(first);
            refreshed.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var second = SessionFrom(refreshed);

            second.Should().NotBe(first);
            (await ListGroupsAsync(second)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Refresh_WithoutACookie_Returns401()
        {
            (await RefreshAsync(null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WithAnUnknownToken_Returns401()
        {
            (await RefreshAsync(UnknownSession)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReplayingRightAway_IsTolerated()
        {
            var first = await SignInAsync();

            (await RefreshAsync(first)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            var replay = await RefreshAsync(first);

            replay.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await RefreshAsync(SessionFrom(replay))).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task ReplayingAfterTheToleranceWindow_RevokesTheWholeSession()
        {
            var first = await SignInAsync();

            var rotated = await RefreshAsync(first);
            rotated.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var second = SessionFrom(rotated);

            await Task.Delay(RefreshToken.ReuseInterval + TimeSpan.FromSeconds(1));

            (await RefreshAsync(first)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await ListGroupsAsync(second)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await RefreshAsync(second)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task EndingTheSession_RejectsTheCookieImmediately()
        {
            var session = await SignInAsync();
            (await ListGroupsAsync(session)).StatusCode.Should().Be(HttpStatusCode.OK);

            var ended = await EndSessionAsync(session);

            ended.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var deletion = SessionSetCookieHeader(ended);
            deletion.Should().StartWith($"{CookieName}=;");
            CookieAttributes(deletion).Should().Contain(new[] { "secure", "path=/" });

            (await ListGroupsAsync(session)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await RefreshAsync(session)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task EndingASession_WithoutACookie_StillSucceeds()
        {
            (await EndSessionAsync(null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task SigningInAgain_RevokesThePreviousSession()
        {
            var username = await RegisterAsync();
            var first = SessionFrom(await StartSessionAsync(username));

            var again = await StartSessionAsync(username, first);
            again.StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await ListGroupsAsync(first)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await ListGroupsAsync(SessionFrom(again))).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task AStateChangingRequest_WithASessionCookie_NeedsTheCsrfHeader()
        {
            var session = await SignInAsync();
            var before = (await GroupsAsync(session)).Count;

            (await CreateGroupAsync(session, withCsrf: false)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GroupsAsync(session)).Should().HaveCount(before);

            (await CreateGroupAsync(session, withCsrf: true)).StatusCode.Should().Be(HttpStatusCode.Created);
            (await GroupsAsync(session)).Should().HaveCount(before + 1);
        }

        [Fact]
        public async Task SessionEndpoints_NeedTheCsrfHeader_EvenWithoutACookie()
        {
            var started = await StartSessionAsync(await RegisterAsync(), withCsrf: false);

            started.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            started.Headers.Contains("Set-Cookie").Should().BeFalse();
        }

        [Fact]
        public async Task AnAuthorizationHeader_NeverFallsBackToTheSessionCookie()
        {
            var session = await SignInAsync();

            var message = CreateGroupMessage();
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", "Zm9vOmJhcg==");

            (await SendAsync(message, session, withCsrf: false)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ACrossOriginPreflight_IsNeverAllowed()
        {
            var preflight = new HttpRequestMessage(HttpMethod.Options, GroupsPath);
            preflight.Headers.Add("Origin", "https://evil.example");
            preflight.Headers.Add("Access-Control-Request-Method", "POST");
            preflight.Headers.Add("Access-Control-Request-Headers", "x-csrf, content-type");

            var response = await NewClient().SendAsync(preflight);

            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
            response.Headers.Contains("Access-Control-Allow-Headers").Should().BeFalse();
            response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
        }

        private HttpClient NewClient() =>
            _fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        private async Task<string> RegisterAsync()
        {
            var username = $"u{Guid.NewGuid():N}"[..20];

            var register = await NewClient().PostAsJsonAsync("/api/v1/auth/register",
                new RegisterRequest(username, $"{username}@test.com", Password, ApiFixture.DisplayName));
            register.StatusCode.Should().Be(HttpStatusCode.Created);

            return username;
        }

        private async Task<string> SignInAsync()
        {
            var started = await StartSessionAsync(await RegisterAsync());
            started.StatusCode.Should().Be(HttpStatusCode.NoContent);

            return SessionFrom(started);
        }

        private Task<HttpResponseMessage> StartSessionAsync(string username, string? session = null, bool withCsrf = true) =>
            SendAsync(
                new HttpRequestMessage(HttpMethod.Post, SessionPath) { Content = JsonContent.Create(new LoginRequest(username, Password)) },
                session,
                withCsrf);

        private Task<HttpResponseMessage> RefreshAsync(string? session) =>
            SendAsync(new HttpRequestMessage(HttpMethod.Post, RefreshPath), session, withCsrf: true);

        private Task<HttpResponseMessage> EndSessionAsync(string? session) =>
            SendAsync(new HttpRequestMessage(HttpMethod.Delete, SessionPath), session, withCsrf: true);

        private Task<HttpResponseMessage> ListGroupsAsync(string session) =>
            SendAsync(new HttpRequestMessage(HttpMethod.Get, GroupsPath), session, withCsrf: false);

        private async Task<List<GroupResponse>> GroupsAsync(string session) =>
            (await (await ListGroupsAsync(session)).Content.ReadFromJsonAsync<List<GroupResponse>>())!;

        private Task<HttpResponseMessage> CreateGroupAsync(string session, bool withCsrf) =>
            SendAsync(CreateGroupMessage(), session, withCsrf);

        private static HttpRequestMessage CreateGroupMessage() =>
            new(HttpMethod.Post, GroupsPath) { Content = JsonContent.Create(new CreateGroupRequest("Piknik", "TRY")) };

        private Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, string? session, bool withCsrf)
        {
            if (session is not null)
                message.Headers.Add("Cookie", $"{CookieName}={session}");

            if (withCsrf)
                message.Headers.Add(CsrfHeader, "1");

            return NewClient().SendAsync(message);
        }

        private static string SessionSetCookieHeader(HttpResponseMessage response) =>
            response.Headers.GetValues("Set-Cookie")
                .Single(h => h.StartsWith($"{CookieName}=", StringComparison.Ordinal));

        private static string SessionFrom(HttpResponseMessage response) =>
            SessionSetCookieHeader(response).Split(';')[0][(CookieName.Length + 1)..];

        private static List<string> CookieAttributes(string setCookieHeader) =>
            setCookieHeader.Split(';').Skip(1).Select(a => a.Trim().ToLowerInvariant()).ToList();
    }
}
