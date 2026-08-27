using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Expenses.Requests;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Api.Contracts.Invitations.Responses;
using Wallet.Api.Contracts.Transfers;

namespace Wallet.IntegrationTests.Api
{
    public class ExpenseFlowTests : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _fixture;

        public ExpenseFlowTests(ApiFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task OneExpenseFiveWays_LeavesThePayerWhole_AndEveryoneElseOwingTheirShare()
        {
            var (groupId, members) = await GroupOfAsync(5);
            var payer = members[0];

            var response = await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, 250m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var balance = await BalanceAsync(payer, groupId);

            Net(balance, payer.Id).Should().Be(200m);
            foreach (var member in members.Skip(1))
                Net(balance, member.Id).Should().Be(-50m);

            balance.Positions.Should().HaveCount(5);
            balance.Positions.Sum(p => p.Net).Should().Be(0m);

            balance.Debts.Should().HaveCount(4);
            balance.Debts.Should().OnlyContain(d => d.CreditorId == payer.Id && d.Amount == 50m);
        }

        [Fact]
        public async Task TwoExpensesTwoPayers_CollapseTheMirroredPairRows_IntoOneRealObligation()
        {
            var (groupId, members) = await GroupOfAsync(6);
            var p1 = members[0];
            var p2 = members[1];

            await PostMarketAndButcherAsync(groupId, members);

            var balance = await BalanceAsync(p1, groupId);

            Net(balance, p1.Id).Should().Be(85m);
            Net(balance, p2.Id).Should().Be(55m);
            foreach (var member in members.Skip(2))
                Net(balance, member.Id).Should().Be(-35m);

            balance.Positions.Sum(p => p.Net).Should().Be(0m);

            balance.Debts.Should()
                .ContainSingle(d => d.DebtorId == p2.Id && d.CreditorId == p1.Id)
                .Which.Amount.Should().Be(5m);
            balance.Debts.Should().NotContain(d => d.DebtorId == p1.Id && d.CreditorId == p2.Id);
        }

        [Fact]
        public async Task TheSimplifiedView_MovesTheSameMoney_WithFewerPayments()
        {
            var (groupId, members) = await GroupOfAsync(6);

            await PostMarketAndButcherAsync(groupId, members);

            var raw = await BalanceAsync(members[0], groupId, simplify: false);
            var simplified = await BalanceAsync(members[0], groupId, simplify: true);

            simplified.Positions.Should().BeEquivalentTo(raw.Positions);

            simplified.Debts.Should().OnlyContain(d => d.Amount > 0m);
            simplified.Debts.Count.Should().BeLessThan(raw.Debts.Count);

            var expected = raw.Positions.Where(p => p.Net != 0m).ToDictionary(p => p.UserId, p => p.Net);

            NetEffect(raw.Debts).Should().BeEquivalentTo(expected);
            NetEffect(simplified.Debts).Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task AFixedShare_AbsorbsTheGenerosity_WithoutTouchingWhoPaid()
        {
            var (groupId, members) = await GroupOfAsync(5);
            var payer = members[0];
            var generous = members[1];

            var participants = members
                .Select(m => new ExpenseParticipantRequest(m.Id, m.Id == generous.Id ? 60m : null))
                .ToList();

            var response = await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, 100m, "Market", DateTimeOffset.UtcNow, participants));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var balance = await BalanceAsync(payer, groupId);

            Net(balance, payer.Id).Should().Be(90m);
            Net(balance, generous.Id).Should().Be(-60m);
            foreach (var member in members.Skip(2))
                Net(balance, member.Id).Should().Be(-10m);

            balance.Positions.Sum(p => p.Net).Should().Be(0m);
        }

        [Fact]
        public async Task AThreeWaySplitOfAnIndivisibleAmount_AllocatesEveryCent_AndStillNetsToZero()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, 100m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            var balance = await BalanceAsync(payer, groupId);

            balance.Positions.Sum(p => p.Net).Should().Be(0m);

            balance.Debts.Should().HaveCount(2);
            balance.Debts.Should().OnlyContain(d => d.CreditorId == payer.Id);
            balance.Debts.Should().OnlyContain(d => d.Amount == 33.33m || d.Amount == 33.34m);
            balance.Debts.Sum(d => d.Amount).Should().Be(Net(balance, payer.Id));
        }

        [Fact]
        public async Task ReplayingAnIdempotencyKey_ReturnsTheSameExpense_AndPostsTheLegsOnlyOnce()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];
            var request = new CreateExpenseRequest(
                groupId, payer.Id, 90m, "Market", DateTimeOffset.UtcNow, Participants(members));
            var key = Guid.NewGuid().ToString();

            var first = await PostExpenseAsync(payer, request, key);
            var second = await PostExpenseAsync(payer, request, key);

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.Created);

            var firstId = await first.Content.ReadFromJsonAsync<Guid>();
            var secondId = await second.Content.ReadFromJsonAsync<Guid>();
            secondId.Should().Be(firstId);

            var balance = await BalanceAsync(payer, groupId);

            Net(balance, payer.Id).Should().Be(60m);
            foreach (var member in members.Skip(1))
                Net(balance, member.Id).Should().Be(-30m);
        }

        [Fact]
        public async Task AParticipantWhoseInvitationIsStillPending_IsRejected_AndNothingIsPosted()
        {
            var payer = await _fixture.RegisterAsync();
            var groupId = await CreateGroupAsync(payer);
            var pending = await _fixture.RegisterAsync();

            var invite = await payer.Client.PostAsJsonAsync(
                $"/api/groups/{groupId}/members", new InviteToGroupRequest(pending.Id));
            invite.StatusCode.Should().Be(HttpStatusCode.NoContent);   // invited, never accepted

            var response = await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, 100m, "Market", DateTimeOffset.UtcNow, Participants(payer, pending)));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var balance = await BalanceAsync(payer, groupId);
            balance.Positions.Should().ContainSingle().Which.Net.Should().Be(0m);
        }

        [Fact]
        public async Task AnExpenseInAGroupYouAreNotIn_Returns404_NotForbidden()
        {
            var (groupId, _) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var response = await PostExpenseAsync(outsider, new CreateExpenseRequest(
                groupId, outsider.Id, 100m, "Market", DateTimeOffset.UtcNow, Participants(outsider)));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task APayerWhoIsNotAmongTheParticipants_IsRejected()
        {
            var (groupId, members) = await GroupOfAsync(3);

            var response = await PostExpenseAsync(members[0], new CreateExpenseRequest(
                groupId, members[0].Id, 100m, "Market", DateTimeOffset.UtcNow,
                Participants(members[1], members[2])));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task FixedSharesBeyondTheTotal_AreRejected()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var participants = members
                .Select(m => new ExpenseParticipantRequest(m.Id, m.Id == members[1].Id ? 120m : null))
                .ToList();

            var response = await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, 100m, "Market", DateTimeOffset.UtcNow, participants));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WithoutAnIdempotencyKeyHeader_TheRequestIsRejected()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var response = await payer.Client.PostAsJsonAsync("/api/expenses", new CreateExpenseRequest(
                groupId, payer.Id, 100m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SettlingInFull_ClearsThePayersDebt_AndRemovesThePairFromTheDebtList()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var creditor = members[0];
            var debtor = members[1];

            await PostExpenseAsync(creditor, new CreateExpenseRequest(
                groupId, creditor.Id, 90m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            var before = await BalanceAsync(debtor, groupId);
            before.Debts.Should()
                .ContainSingle(d => d.DebtorId == debtor.Id && d.CreditorId == creditor.Id)
                .Which.Amount.Should().Be(30m);

            var settle = await SettleAsync(debtor, groupId, creditor, 30m);
            settle.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var after = await BalanceAsync(debtor, groupId);

            Net(after, debtor.Id).Should().Be(0m);
            Net(after, creditor.Id).Should().Be(30m);
            after.Debts.Should().NotContain(d => d.DebtorId == debtor.Id || d.CreditorId == debtor.Id);
            after.Positions.Sum(p => p.Net).Should().Be(0m);
        }

        [Fact]
        public async Task SettlingPartially_LeavesTheRemainderStanding()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var creditor = members[0];
            var debtor = members[1];

            await PostExpenseAsync(creditor, new CreateExpenseRequest(
                groupId, creditor.Id, 90m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            await SettleAsync(debtor, groupId, creditor, 10m);

            var after = await BalanceAsync(debtor, groupId);

            Net(after, debtor.Id).Should().Be(-20m);
            after.Debts.Should()
                .ContainSingle(d => d.DebtorId == debtor.Id && d.CreditorId == creditor.Id)
                .Which.Amount.Should().Be(20m);
        }

        [Fact]
        public async Task OverSettling_TurnsTheDebtorIntoTheCreditor()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var creditor = members[0];
            var debtor = members[1];

            await PostExpenseAsync(creditor, new CreateExpenseRequest(
                groupId, creditor.Id, 90m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            await SettleAsync(debtor, groupId, creditor, 50m);

            var after = await BalanceAsync(debtor, groupId);

            Net(after, debtor.Id).Should().Be(20m);
            Net(after, creditor.Id).Should().Be(10m);
            after.Debts.Should()
                .ContainSingle(d => d.DebtorId == creditor.Id && d.CreditorId == debtor.Id)
                .Which.Amount.Should().Be(20m);
            after.Positions.Sum(p => p.Net).Should().Be(0m);
        }

        private static async Task<Guid> CreateGroupAsync(TestUser owner, string currency = "TRY")
        {
            var created = await owner.Client.PostAsJsonAsync(
                "/api/groups", new CreateGroupRequest("Piknik", currency));
            created.StatusCode.Should().Be(HttpStatusCode.Created);

            return await created.Content.ReadFromJsonAsync<Guid>();
        }

        private static async Task JoinAsync(TestUser inviter, Guid groupId, TestUser invitee)
        {
            var invite = await inviter.Client.PostAsJsonAsync(
                $"/api/groups/{groupId}/members", new InviteToGroupRequest(invitee.Id));
            invite.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var invitations = await invitee.Client.GetFromJsonAsync<List<InvitationResponse>>("/api/invitations");
            var invitation = invitations!.Single(i => i.GroupId == groupId);

            var accept = await invitee.Client.PostAsync($"/api/invitations/{invitation.Id}/accept", null);
            accept.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private async Task<(Guid GroupId, TestUser[] Members)> GroupOfAsync(int size)
        {
            var members = new TestUser[size];
            members[0] = await _fixture.RegisterAsync();

            var groupId = await CreateGroupAsync(members[0]);

            for (var i = 1; i < size; i++)
            {
                members[i] = await _fixture.RegisterAsync();
                await JoinAsync(members[0], groupId, members[i]);
            }

            return (groupId, members);
        }

        private static async Task PostMarketAndButcherAsync(Guid groupId, TestUser[] members)
        {
            var everyone = Participants(members);

            var market = await PostExpenseAsync(members[0], new CreateExpenseRequest(
                groupId, members[0].Id, 120m, "Market", DateTimeOffset.UtcNow, everyone));
            market.StatusCode.Should().Be(HttpStatusCode.Created);

            var butcher = await PostExpenseAsync(members[1], new CreateExpenseRequest(
                groupId, members[1].Id, 90m, "Kasap", DateTimeOffset.UtcNow, everyone));
            butcher.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        private static async Task<HttpResponseMessage> PostExpenseAsync(
            TestUser sender, CreateExpenseRequest request, string? idempotencyKey = null)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/expenses")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());

            return await sender.Client.SendAsync(message);
        }

        private static async Task<HttpResponseMessage> SettleAsync(
            TestUser payer, Guid groupId, TestUser payee, decimal amount, string? idempotencyKey = null)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
            {
                Content = JsonContent.Create(new TransferRequest(groupId, payee.Id, amount))
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());

            return await payer.Client.SendAsync(message);
        }

        private static async Task<GroupBalanceResponse> BalanceAsync(
            TestUser user, Guid groupId, bool simplify = false)
        {
            var balance = await user.Client.GetFromJsonAsync<GroupBalanceResponse>(
                $"/api/groups/{groupId}/balance?simplify={(simplify ? "true" : "false")}");

            return balance!;
        }

        private static List<ExpenseParticipantRequest> Participants(params TestUser[] users) =>
            users.Select(u => new ExpenseParticipantRequest(u.Id, null)).ToList();

        private static decimal Net(GroupBalanceResponse balance, Guid userId) =>
            balance.Positions.Single(p => p.UserId == userId).Net;

        private static Dictionary<Guid, decimal> NetEffect(IReadOnlyList<PairwiseDebtResponse> debts)
        {
            var effect = new Dictionary<Guid, decimal>();

            foreach (var debt in debts)
            {
                effect[debt.DebtorId] = effect.GetValueOrDefault(debt.DebtorId) - debt.Amount;
                effect[debt.CreditorId] = effect.GetValueOrDefault(debt.CreditorId) + debt.Amount;
            }

            return effect;
        }
    }
}