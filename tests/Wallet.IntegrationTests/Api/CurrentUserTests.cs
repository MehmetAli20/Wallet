using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Users;

namespace Wallet.IntegrationTests.Api
{
    public class CurrentUserTests : IClassFixture<ApiFixture>
    {
        private const string MePath = "/api/v1/users/me";

        private readonly ApiFixture _fixture;

        public CurrentUserTests(ApiFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task Me_ReturnsTheCallersIdAndDisplayName_AndIsNeverCached()
        {
            var user = await _fixture.RegisterAsync();

            var response = await user.Client.GetAsync(MePath);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.CacheControl!.NoStore.Should().BeTrue();

            var me = (await response.Content.ReadFromJsonAsync<CurrentUserResponse>())!;
            me.Id.Should().Be(user.Id);
            me.DisplayName.Should().Be(ApiFixture.DisplayName);
        }

        [Fact]
        public async Task Me_WithoutCredentials_Returns401()
        {
            (await _fixture.CreateClient().GetAsync(MePath)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Renaming_NormalizesTheName_AndMeReflectsIt()
        {
            var user = await _fixture.RegisterAsync();

            var renamed = await user.Client.PatchAsJsonAsync(MePath, new UpdateCurrentUserRequest("  Ayşe   Yılmaz  "));

            renamed.StatusCode.Should().Be(HttpStatusCode.OK);
            (await renamed.Content.ReadFromJsonAsync<CurrentUserResponse>())!.DisplayName.Should().Be("Ayşe Yılmaz");
            (await user.Client.GetFromJsonAsync<CurrentUserResponse>(MePath))!.DisplayName.Should().Be("Ayşe Yılmaz");
        }

        [Fact]
        public async Task Renaming_ToAnInvalidName_Returns400()
        {
            var user = await _fixture.RegisterAsync();

            var renamed = await user.Client.PatchAsJsonAsync(MePath, new UpdateCurrentUserRequest("‮eşyA"));

            renamed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
