using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Expenses.Requests;
using Wallet.Api.Contracts.Expenses.Responses;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Domain.Expenses;
using Wallet.Api.Contracts.Common;

namespace Wallet.IntegrationTests.Api
{
    public class RecurringExpenseEndpointTests : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _fixture;

        public RecurringExpenseEndpointTests(ApiFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task ReplayingTheSameKey_DoesNotSetUpTheRentTwice()
        {
            var (groupId, owner) = await GroupOfOneAsync();
            var request = NewRequest(groupId, owner.Id);
            var key = Guid.NewGuid().ToString();

            var first = await PostAsync(owner, request, key);
            var second = await PostAsync(owner, request, key);

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.Created);

            ((await second.Content.ReadFromJsonAsync<CreatedResponse>())!.Id)
                .Should().Be((await first.Content.ReadFromJsonAsync<CreatedResponse>())!.Id);

            var mine = await owner.Client.GetFromJsonAsync<List<RecurringExpenseResponse>>(
                "/api/v1/recurringexpenses");

            mine!.Where(r => r.GroupId == groupId).Should().ContainSingle();
        }

        [Fact]
        public async Task ADifferentKey_SetsUpASecondOne()
        {
            var (groupId, owner) = await GroupOfOneAsync();
            var request = NewRequest(groupId, owner.Id);

            await PostAsync(owner, request, Guid.NewGuid().ToString());
            await PostAsync(owner, request, Guid.NewGuid().ToString());

            var mine = await owner.Client.GetFromJsonAsync<List<RecurringExpenseResponse>>(
                "/api/v1/recurringexpenses");

            mine!.Where(r => r.GroupId == groupId).Should().HaveCount(2);
        }

        [Fact]
        public async Task WithoutAnIdempotencyKey_TheRequestIsRejected()
        {
            var (groupId, owner) = await GroupOfOneAsync();

            var response = await owner.Client.PostAsJsonAsync(
                "/api/v1/recurringexpenses", NewRequest(groupId, owner.Id));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CancellingIt_TakesItOffTheList()
        {
            var (groupId, owner) = await GroupOfOneAsync();

            var created = await PostAsync(
                owner, NewRequest(groupId, owner.Id), Guid.NewGuid().ToString());

            var id = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

            var cancelled = await owner.Client.DeleteAsync($"/api/v1/recurringexpenses/{id}");
            cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var mine = await owner.Client.GetFromJsonAsync<List<RecurringExpenseResponse>>(
                "/api/v1/recurringexpenses");

            mine!.Should().NotContain(r => r.Id == id);
        }

        private static CreateRecurringExpenseRequest NewRequest(Guid groupId, Guid payerId) =>
            new(groupId,
                payerId,
                900m,
                "Kira",
                RecurrenceInterval.Monthly,
                new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
                new List<ExpenseParticipantRequest> { new(payerId, null) });

        private static async Task<HttpResponseMessage> PostAsync(
            TestUser owner, CreateRecurringExpenseRequest request, string key)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/recurringexpenses")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("Idempotency-Key", key);

            return await owner.Client.SendAsync(message);
        }

        private async Task<(Guid GroupId, TestUser Owner)> GroupOfOneAsync()
        {
            var owner = await _fixture.RegisterAsync();

            var created = await owner.Client.PostAsJsonAsync(
                "/api/v1/groups", new CreateGroupRequest("Ev", "TRY"));

            created.StatusCode.Should().Be(HttpStatusCode.Created);

            return ((await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id, owner);
        }
    }
}
