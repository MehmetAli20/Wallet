using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Accounts.Responses;
using Wallet.Api.Contracts.Common;
using Wallet.Api.Contracts.Expenses.Requests;
using Wallet.Api.Contracts.Expenses.Responses;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Api.Contracts.Invitations.Responses;

namespace Wallet.IntegrationTests.Api
{
    public class CreateExpenseEndpointTests : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _fixture;

        public CreateExpenseEndpointTests(ApiFixture fixture) => _fixture = fixture;

        private sealed record Trio(TestUser Payer, TestUser First, TestUser Second, Guid GroupId);

        private async Task<Trio> SeedGroupOfThreeAsync()
        {
            var payer = await _fixture.RegisterAsync();
            var first = await _fixture.RegisterAsync();
            var second = await _fixture.RegisterAsync();

            var created = await payer.Client.PostAsJsonAsync(
                "/api/v1/groups", new CreateGroupRequest("Piknik", "TRY"));
            created.StatusCode.Should().Be(HttpStatusCode.Created);

            var groupId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

            foreach (var member in new[] { first, second })
            {
                var invited = await payer.Client.PostAsJsonAsync(
                    $"/api/v1/groups/{groupId}/members", new InviteToGroupRequest(member.Id));
                invited.StatusCode.Should().Be(HttpStatusCode.NoContent);

                var invitations = await member.Client
                    .GetFromJsonAsync<List<InvitationResponse>>("/api/v1/invitations");

                var invitationId = invitations!.Single(i => i.GroupId == groupId).Id;

                var accepted = await member.Client.PostAsync(
                    $"/api/v1/invitations/{invitationId}/accept", null);
                accepted.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            return new Trio(payer, first, second, groupId);
        }

        private static async Task<HttpResponseMessage> PostExpenseAsync(
            HttpClient client, CreateExpenseRequest request, string idempotencyKey)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/expenses")
            {
                Content = JsonContent.Create(request)
            };

            message.Headers.Add("Idempotency-Key", idempotencyKey);

            return await client.SendAsync(message);
        }

        private static CreateExpenseRequest EqualSplit(Trio trio, decimal amount) =>
            new(trio.GroupId, trio.Payer.Id, amount, "Aksam yemegi", DateTimeOffset.UtcNow,
                new[]
                {
                    new ExpenseParticipantRequest(trio.Payer.Id, null),
                    new ExpenseParticipantRequest(trio.First.Id, null),
                    new ExpenseParticipantRequest(trio.Second.Id, null)
                });

        private static async Task<GroupBalanceResponse> GetBalanceAsync(
            TestUser user, Guid groupId, bool simplify = false)
        {
            var balance = await user.Client.GetFromJsonAsync<GroupBalanceResponse>(
                $"/api/v1/groups/{groupId}/balance?simplify={simplify}");

            return balance!;
        }

        private static string NewKey() => Guid.NewGuid().ToString();

        [Fact]
        public async Task CreateExpense_SplitsEquallyAcrossParticipants()
        {
            var trio = await SeedGroupOfThreeAsync();

            var response = await PostExpenseAsync(trio.Payer.Client, EqualSplit(trio, 90m), NewKey());

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id.Should().NotBeEmpty();

            var balance = await GetBalanceAsync(trio.Payer, trio.GroupId);

            balance.Currency.Should().Be("TRY");
            balance.Positions.Should().HaveCount(3);
            balance.Positions.Single(p => p.UserId == trio.Payer.Id).Net.Should().Be(60m);
            balance.Positions.Single(p => p.UserId == trio.First.Id).Net.Should().Be(-30m);
            balance.Positions.Single(p => p.UserId == trio.Second.Id).Net.Should().Be(-30m);
            balance.Positions.Sum(p => p.Net).Should().Be(0m);
        }

        [Fact]
        public async Task CreateExpense_UpdatesEachParticipantsOwnAccount()
        {
            var trio = await SeedGroupOfThreeAsync();

            await PostExpenseAsync(trio.Payer.Client, EqualSplit(trio, 90m), NewKey());

            var payerAccounts = await trio.Payer.Client
                .GetFromJsonAsync<List<AccountListResponse>>("/api/v1/accounts");
            var debtorAccounts = await trio.First.Client
                .GetFromJsonAsync<List<AccountListResponse>>("/api/v1/accounts");

            payerAccounts!.Single(a => a.Currency == "TRY").Balance.Should().Be(60m);
            debtorAccounts!.Single(a => a.Currency == "TRY").Balance.Should().Be(-30m);
        }

        [Fact]
        public async Task CreateExpense_WithAFixedShare_SplitsOnlyTheRemainder()
        {
            var trio = await SeedGroupOfThreeAsync();

            var request = new CreateExpenseRequest(
                trio.GroupId, trio.Payer.Id, 100m, "Market", DateTimeOffset.UtcNow,
                new[]
                {
                    new ExpenseParticipantRequest(trio.Payer.Id, null),
                    new ExpenseParticipantRequest(trio.First.Id, 40m),
                    new ExpenseParticipantRequest(trio.Second.Id, null)
                });

            var response = await PostExpenseAsync(trio.Payer.Client, request, NewKey());
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var balance = await GetBalanceAsync(trio.Payer, trio.GroupId);

            balance.Positions.Single(p => p.UserId == trio.First.Id).Net.Should().Be(-40m);
            balance.Positions.Single(p => p.UserId == trio.Second.Id).Net.Should().Be(-30m);
            balance.Positions.Single(p => p.UserId == trio.Payer.Id).Net.Should().Be(70m);
        }

        [Fact]
        public async Task CreateExpense_WithAnUnevenTotal_AllocatesEveryCent()
        {
            var trio = await SeedGroupOfThreeAsync();

            await PostExpenseAsync(trio.Payer.Client, EqualSplit(trio, 100m), NewKey());

            var expenses = await trio.Payer.Client
                .GetFromJsonAsync<List<ExpenseResponse>>($"/api/v1/groups/{trio.GroupId}/expenses");

            var splits = expenses!.Single().Splits;

            splits.Should().HaveCount(3);
            splits.Sum(s => s.Share).Should().Be(100m);
            splits.Should().OnlyContain(s => s.Share == 33.33m || s.Share == 33.34m);

            var balance = await GetBalanceAsync(trio.Payer, trio.GroupId);

            balance.Positions.Sum(p => p.Net).Should().Be(0m);
        }

        [Fact]
        public async Task CreateExpense_ReplayedWithTheSameKey_PostsOnlyOnce()
        {
            var trio = await SeedGroupOfThreeAsync();
            var request = EqualSplit(trio, 90m);
            var key = NewKey();

            var first = await PostExpenseAsync(trio.Payer.Client, request, key);
            var second = await PostExpenseAsync(trio.Payer.Client, request, key);

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.Created);

            var firstId = (await first.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
            var secondId = (await second.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

            secondId.Should().Be(firstId);

            var expenses = await trio.Payer.Client
                .GetFromJsonAsync<List<ExpenseResponse>>($"/api/v1/groups/{trio.GroupId}/expenses");

            expenses.Should().ContainSingle();

            var balance = await GetBalanceAsync(trio.Payer, trio.GroupId);
            balance.Positions.Single(p => p.UserId == trio.Payer.Id).Net.Should().Be(60m);
        }

        [Fact]
        public async Task CreateExpense_ByAnOutsider_Returns404()
        {
            var trio = await SeedGroupOfThreeAsync();
            var outsider = await _fixture.RegisterAsync();

            var response = await PostExpenseAsync(outsider.Client, EqualSplit(trio, 90m), NewKey());

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateExpense_WithAPayerWhoIsNotAParticipant_Returns400()
        {
            var trio = await SeedGroupOfThreeAsync();

            var request = new CreateExpenseRequest(
                trio.GroupId, trio.Payer.Id, 60m, "Taksi", DateTimeOffset.UtcNow,
                new[]
                {
                    new ExpenseParticipantRequest(trio.First.Id, null),
                    new ExpenseParticipantRequest(trio.Second.Id, null)
                });

            var response = await PostExpenseAsync(trio.Payer.Client, request, NewKey());

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GroupExpenses_ShowTheViewersOwnShare()
        {
            var trio = await SeedGroupOfThreeAsync();

            await PostExpenseAsync(trio.Payer.Client, EqualSplit(trio, 90m), NewKey());

            var seenByDebtor = await trio.First.Client
                .GetFromJsonAsync<List<ExpenseResponse>>($"/api/v1/groups/{trio.GroupId}/expenses");

            var expense = seenByDebtor!.Single();

            expense.PayerId.Should().Be(trio.Payer.Id);
            expense.Amount.Should().Be(90m);
            expense.Currency.Should().Be("TRY");
            expense.MyShare.Should().Be(30m);
            expense.IsReversed.Should().BeFalse();
        }

        [Fact]
        public async Task GroupBalance_Simplified_PointsEveryDebtAtThePayer()
        {
            var trio = await SeedGroupOfThreeAsync();

            await PostExpenseAsync(trio.Payer.Client, EqualSplit(trio, 90m), NewKey());

            var balance = await GetBalanceAsync(trio.Payer, trio.GroupId, simplify: true);

            balance.Debts.Should().HaveCount(2);
            balance.Debts.Should().OnlyContain(d => d.CreditorId == trio.Payer.Id && d.Amount == 30m);
            balance.Debts.Select(d => d.DebtorId)
                .Should().BeEquivalentTo(new[] { trio.First.Id, trio.Second.Id });
        }
    }
}