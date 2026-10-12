using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Wallet.Api.Contracts.Users;

namespace Wallet.IntegrationTests.Api
{
    public class NativeSessionTests : IClassFixture<ApiFixture>
    {
        private const string Password = "password123";

        private readonly ApiFixture _fixture;

        public NativeSessionTests(ApiFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task Login_ReturnsAWorkingTokenPair()
        {
            var tokens = await SignInAsync(await RegisterAsync());

            tokens.RefreshToken.Should().StartWith("wrt_");
            tokens.AccessTokenExpiresAt.Should().BeBefore(tokens.RefreshTokenExpiresAt);
            (await MeAsync(tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Refresh_RotatesTheRefreshToken_AndIssuesAWorkingAccessToken()
        {
            var first = await SignInAsync(await RegisterAsync());

            var refreshed = await RefreshAsync(first.RefreshToken);
            refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = (await refreshed.Content.ReadFromJsonAsync<TokenResponse>())!;

            second.RefreshToken.Should().NotBe(first.RefreshToken);
            (await MeAsync(second.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Logout_KillsTheAccessTokenImmediately()
        {
            var tokens = await SignInAsync(await RegisterAsync());

            (await LogoutAsync(tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await MeAsync(tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await RefreshAsync(tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task LoggingOutRightAfterARotation_CannotBeUndoneByReplayingThePreviousToken()
        {
            var first = await SignInAsync(await RegisterAsync());

            var second = (await (await RefreshAsync(first.RefreshToken)).Content.ReadFromJsonAsync<TokenResponse>())!;

            (await LogoutAsync(second.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await RefreshAsync(first.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task EndingAllSessions_EndsEverySessionOfTheUser()
        {
            var email = await RegisterAsync();
            var phone = await SignInAsync(email);
            var tablet = await SignInAsync(email);

            var endAll = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me/sessions");
            endAll.Headers.Authorization = new AuthenticationHeaderValue("Bearer", phone.AccessToken);
            (await _fixture.CreateClient().SendAsync(endAll)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await MeAsync(phone.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await MeAsync(tablet.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await RefreshAsync(tablet.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AnAccessTokenWithoutASession_IsRejected()
        {
            var tokens = await SignInAsync(await RegisterAsync());
            var subject = (await (await MeAsync(tokens.AccessToken)).Content.ReadFromJsonAsync<CurrentUserResponse>())!.Id;

            var unbound = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "wallet-tests",
                Audience = "wallet-tests",
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wallet-integration-tests-signing-key-32b+")),
                    SecurityAlgorithms.HmacSha256),
                Claims = new Dictionary<string, object>
                {
                    [JwtRegisteredClaimNames.Sub] = subject.ToString(),
                    [ClaimTypes.Role] = "User"
                }
            });

            (await MeAsync(unbound)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private async Task<string> RegisterAsync()
        {
            var email = $"u{Guid.NewGuid():N}@test.com";

            var register = await _fixture.CreateClient().PostAsJsonAsync("/api/v1/auth/register",
                new RegisterRequest(email, Password, ApiFixture.DisplayName));
            register.StatusCode.Should().Be(HttpStatusCode.Created);

            return email;
        }

        private async Task<TokenResponse> SignInAsync(string email)
        {
            var login = await _fixture.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, Password));
            login.StatusCode.Should().Be(HttpStatusCode.OK);

            return (await login.Content.ReadFromJsonAsync<TokenResponse>())!;
        }

        private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
            _fixture.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(refreshToken));

        private Task<HttpResponseMessage> LogoutAsync(string refreshToken) =>
            _fixture.CreateClient().PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest(refreshToken));

        private Task<HttpResponseMessage> MeAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return _fixture.CreateClient().SendAsync(request);
        }
    }
}
