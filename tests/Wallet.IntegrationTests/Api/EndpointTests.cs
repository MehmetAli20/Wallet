using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Api.Contracts.Common;

namespace Wallet.IntegrationTests.Api
{
    public class EndpointTests : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _fixture;

        public EndpointTests(ApiFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task ProtectedEndpoint_WithoutToken_Returns401()
        {
            var client = _fixture.CreateClient();

            var response = await client.GetAsync("/api/groups");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Health_IsAnonymous()
        {
            var client = _fixture.CreateClient();

            var response = await client.GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RegisterThenLogin_ReturnsAUsableToken()
        {
            var client = (await _fixture.RegisterAsync()).Client;

            var response = await client.GetAsync("/api/groups");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateGroup_ThenListGroups_ReturnsIt()
        {
            var client = (await _fixture.RegisterAsync()).Client;

            var created = await client.PostAsJsonAsync("/api/groups", new CreateGroupRequest("Piknik", "TRY"));
            created.StatusCode.Should().Be(HttpStatusCode.Created);

            var groupId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

            var groups = await client.GetFromJsonAsync<List<GroupResponse>>("/api/groups");

            groups.Should().ContainSingle(g => g.Id == groupId);
            groups!.Single(g => g.Id == groupId).Currency.Should().Be("TRY");
        }

        [Fact]
        public async Task CreateGroup_WithInvalidPayload_Returns400()
        {
            var client = (await _fixture.RegisterAsync()).Client;

            var response = await client.PostAsJsonAsync("/api/groups", new CreateGroupRequest("", "TRY"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GroupBalance_RouteParameterBinds()
        {
            var client = (await _fixture.RegisterAsync()).Client;

            var created = await client.PostAsJsonAsync("/api/groups", new CreateGroupRequest("Piknik", "TRY"));
            var groupId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

            var balance = await client.GetFromJsonAsync<GroupBalanceResponse>($"/api/groups/{groupId}/balance");

            balance!.GroupId.Should().Be(groupId);
            balance.Currency.Should().Be("TRY");
            balance.Positions.Should().ContainSingle();
        }

        [Fact]
        public async Task GroupBalance_ForAGroupYouAreNotIn_Returns404()
        {
            var client = (await _fixture.RegisterAsync()).Client;

            var response = await client.GetAsync($"/api/groups/{Guid.NewGuid()}/balance");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
