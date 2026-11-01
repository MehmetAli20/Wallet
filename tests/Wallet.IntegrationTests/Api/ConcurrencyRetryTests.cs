using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Common;
using Wallet.Api.Contracts.Expenses.Requests;
using Wallet.Api.Contracts.Expenses.Responses;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;

namespace Wallet.IntegrationTests.Api
{
    public class ConcurrencyRetryTests : IClassFixture<ConflictOnceApiFixture>
    {
        private readonly ConflictOnceApiFixture _fixture;

        public ConcurrencyRetryTests(ConflictOnceApiFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task AnExpenseSavedOnTheRetry_IsRecordedOnce_AndReturns201()
        {
            var payer = await _fixture.RegisterAsync();
            var friend = await _fixture.RegisterAsync();
            var groupId = await GroupOfTwoAsync(payer, friend);

            _fixture.FailNextSave();

            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/expenses")
            {
                Content = JsonContent.Create(new CreateExpenseRequest(
                    groupId, payer.Id, 90m, "Market", DateTimeOffset.UtcNow,
                    [new ExpenseParticipantRequest(payer.Id, null), new ExpenseParticipantRequest(friend.Id, null)]))
            };
            message.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

            var response = await payer.Client.SendAsync(message);

            var expenses = (await payer.Client.GetFromJsonAsync<List<ExpenseResponse>>(
                $"/api/v1/groups/{groupId}/expenses"))!;

            expenses.Should().ContainSingle();
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id.Should().Be(expenses[0].Id);
        }

        private static async Task<Guid> GroupOfTwoAsync(TestUser owner, TestUser friend)
        {
            var created = await owner.Client.PostAsJsonAsync(
                "/api/v1/groups", new CreateGroupRequest("Trip", "USD"));
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            var groupId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

            var issued = await owner.Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/invite-links", new IssueInviteLinkRequest(null, null));
            issued.StatusCode.Should().Be(HttpStatusCode.OK);
            var link = (await issued.Content.ReadFromJsonAsync<IssuedTokenResponse>())!;

            var joined = await friend.Client.PostAsJsonAsync(
                "/api/v1/groups/invite-links/join", new JoinGroupRequest(link.Token));
            joined.StatusCode.Should().Be(HttpStatusCode.OK);

            return groupId;
        }
    }
}
