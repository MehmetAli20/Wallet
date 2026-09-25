using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wallet.Api.Contracts.Accounts.Responses;
using Wallet.Api.Contracts.Activity.Responses;
using Wallet.Api.Contracts.Expenses.Requests;
using Wallet.Api.Contracts.Expenses.Responses;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Api.Contracts.Invitations.Responses;
using Wallet.Api.Contracts.Placeholders;
using Wallet.Api.Contracts.Users;
using Wallet.Api.Contracts.Transfers;
using Wallet.Api.Contracts.Common;

namespace Wallet.IntegrationTests.Api
{
    public class GroupFlowTests : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _fixture;

        public GroupFlowTests(ApiFixture fixture) => _fixture = fixture;

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

            var firstId = (await first.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
            var secondId = (await second.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
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
                $"/api/v1/groups/{groupId}/members", new InviteToGroupRequest(pending.Id));
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

            var response = await payer.Client.PostAsJsonAsync("/api/v1/expenses", new CreateExpenseRequest(
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

        [Fact]
        public async Task ReversingAnExpense_ClearsEveryPosition()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var expenseId = await CreateExpenseAsync(payer, groupId, 90m, members);

            var reversal = await ReverseAsync(members[1], expenseId, "wrong amount");
            reversal.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var after = await BalanceAsync(payer, groupId);

            after.Positions.Should().OnlyContain(pos => pos.Net == 0m);
            after.Debts.Should().BeEmpty();
        }

        [Fact]
        public async Task ReversingAnExpense_AppendsEntries_AndDeletesNone()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var expenseId = await CreateExpenseAsync(payer, groupId, 90m, members);
            var before = await LedgerEntryCountAsync(payer);

            await ReverseAsync(payer, expenseId);

            var after = await LedgerEntryCountAsync(payer);

            after.Should().BeGreaterThan(before);
        }

        [Fact]
        public async Task ReversingTwice_IsRejected()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var expenseId = await CreateExpenseAsync(payer, groupId, 90m, members);

            (await ReverseAsync(payer, expenseId)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await ReverseAsync(payer, expenseId)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ReversingAnExpenseInAGroupYouAreNotIn_Returns404()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var expenseId = await CreateExpenseAsync(members[0], groupId, 50m, members);

            var outsider = await _fixture.RegisterAsync();

            var response = await ReverseAsync(outsider, expenseId);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ReversingAnExpenseThatWasAlreadySettled_TurnsTheCreditorIntoTheDebtor()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var creditor = members[0];
            var debtor = members[1];

            var expenseId = await CreateExpenseAsync(creditor, groupId, 90m, members);

            await SettleAsync(debtor, groupId, creditor, 30m);
            await ReverseAsync(creditor, expenseId);

            var after = await BalanceAsync(creditor, groupId);

            Net(after, debtor.Id).Should().Be(30m);
            Net(after, creditor.Id).Should().Be(-30m);
            Net(after, members[2].Id).Should().Be(0m);
            after.Positions.Sum(pos => pos.Net).Should().Be(0m);
        }

        [Fact]
        public async Task RevisingAnExpense_ReversesTheOriginal_AndPostsTheNewAmounts()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var expenseId = await CreateExpenseAsync(payer, groupId, 90m, members);

            var response = await ReviseAsync(payer, expenseId, new ReviseExpenseRequest(
                60m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var revisionId = (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
            revisionId.Should().NotBe(expenseId);

            var after = await BalanceAsync(payer, groupId);

            Net(after, payer.Id).Should().Be(40m);
            foreach (var member in members.Skip(1))
                Net(after, member.Id).Should().Be(-20m);

            after.Positions.Sum(pos => pos.Net).Should().Be(0m);
        }

        [Fact]
        public async Task RevisingAsSomeoneOtherThanThePayer_IsRejected()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];
            var freeloader = members[1];

            var expenseId = await CreateExpenseAsync(payer, groupId, 90m, members);

            var response = await ReviseAsync(freeloader, expenseId, new ReviseExpenseRequest(
                90m, "Market", DateTimeOffset.UtcNow, Participants(payer, members[2])));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var after = await BalanceAsync(payer, groupId);
            Net(after, freeloader.Id).Should().Be(-30m);
        }

        [Fact]
        public async Task RevisingTheSameExpenseTwice_IsRejected()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var expenseId = await CreateExpenseAsync(payer, groupId, 90m, members);

            var request = new ReviseExpenseRequest(
                60m, "Market", DateTimeOffset.UtcNow, Participants(members));

            (await ReviseAsync(payer, expenseId, request)).StatusCode.Should().Be(HttpStatusCode.Created);
            (await ReviseAsync(payer, expenseId, request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task TheActivityFeed_NamesWhoEnteredTheExpense_NotWhoPaid()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];
            var author = members[1];

            var created = await PostExpenseAsync(author, new CreateExpenseRequest(
                groupId, payer.Id, 90m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            created.StatusCode.Should().Be(HttpStatusCode.Created);
            var expenseId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

            var feed = await ActivityAsync(payer, groupId);

            var entry = feed.Should().ContainSingle(e => e.Type == "ExpenseCreated").Subject;

            entry.ActorId.Should().Be(author.Id);
            entry.ActorId.Should().NotBe(payer.Id);
            entry.SubjectId.Should().Be(expenseId);
            entry.Amount.Should().Be(90m);
            entry.Description.Should().Be("Market");
        }

        [Fact]
        public async Task ReversalAndSettlement_BothLandInTheFeed()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var creditor = members[0];
            var debtor = members[1];

            var expenseId = await CreateExpenseAsync(creditor, groupId, 90m, members);

            await SettleAsync(debtor, groupId, creditor, 30m);
            await ReverseAsync(debtor, expenseId, "never happened");

            var feed = await ActivityAsync(creditor, groupId);

            feed.Should().ContainSingle(e => e.Type == "SettlementRecorded")
                .Which.ActorId.Should().Be(debtor.Id);

            var reversal = feed.Should().ContainSingle(e => e.Type == "ExpenseReversed").Subject;
            reversal.ActorId.Should().Be(debtor.Id);
            reversal.SubjectId.Should().Be(expenseId);
            reversal.Description.Should().Be("never happened");
        }

        [Fact]
        public async Task TheFeedIsOrdered_AndTheCursorSkipsWhatYouHaveSeen()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            await CreateExpenseAsync(payer, groupId, 90m, members);

            var first = await ActivityAsync(payer, groupId);
            var newest = first.Max(e => e.Sequence);

            first.Select(e => e.Sequence).Should().BeInDescendingOrder();

            await CreateExpenseAsync(payer, groupId, 60m, members);

            var since = await ActivityAsync(payer, groupId, after: newest);

            since.Should().ContainSingle();
            since[0].Amount.Should().Be(60m);
        }

        [Fact]
        public async Task TheActivityFeedOfAGroupYouAreNotIn_Returns404()
        {
            var (groupId, _) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var response = await outsider.Client.GetAsync($"/api/v1/groups/{groupId}/activity");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task InvitingAndJoining_AreBothVisibleInTheFeed()
        {
            var (groupId, members) = await GroupOfAsync(2);

            var feed = await ActivityAsync(members[0], groupId);

            feed.Should().ContainSingle(e => e.Type == "MemberInvited")
                .Which.SubjectId.Should().Be(members[1].Id);

            feed.Should().ContainSingle(e => e.Type == "MemberJoined")
                .Which.ActorId.Should().Be(members[1].Id);
        }

        [Fact]
        public async Task AnInvitation_ShowsTheInvitersDisplayName()
        {
            var inviter = await _fixture.RegisterAsync();
            var invitee = await _fixture.RegisterAsync();
            var groupId = await CreateGroupAsync(inviter);

            var invite = await inviter.Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/members", new InviteToGroupRequest(invitee.Id));
            invite.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var invitation = (await invitee.Client.GetFromJsonAsync<List<InvitationResponse>>("/api/v1/invitations"))!
                .Single(i => i.GroupId == groupId);

            invitation.InvitedByUserId.Should().Be(inviter.Id);
            invitation.InvitedByDisplayName.Should().Be(ApiFixture.DisplayName);
        }

        [Fact]
        public async Task YourOwnActionsAreNeverUnread()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var author = members[0];

            var before = await UnreadCountAsync(author, groupId);

            await CreateExpenseAsync(author, groupId, 90m, members);

            var after = await UnreadCountAsync(author, groupId);

            after.Should().Be(before);
        }

        [Fact]
        public async Task SomeoneElsesExpense_ShowsUpAsUnread_UntilYouMarkItSeen()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var author = members[0];
            var reader = members[1];

            await CreateExpenseAsync(author, groupId, 90m, members);

            var before = await UnreadAsync(reader);
            before.Should().ContainSingle(u => u.GroupId == groupId)
                .Which.Unread.Should().BeGreaterThan(0);

            var feed = await ActivityAsync(reader, groupId);
            var newest = feed.Max(e => e.Sequence);

            var seen = await MarkSeenAsync(reader, groupId, newest);
            seen.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var after = await UnreadAsync(reader);
            after.Should().NotContain(u => u.GroupId == groupId);
        }

        [Fact]
        public async Task WhatHappensAfterYouLooked_BecomesUnreadAgain()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var author = members[0];
            var reader = members[1];

            await CreateExpenseAsync(author, groupId, 90m, members);

            var feed = await ActivityAsync(reader, groupId);
            await MarkSeenAsync(reader, groupId, feed.Max(e => e.Sequence));

            await CreateExpenseAsync(author, groupId, 60m, members);

            var after = await UnreadAsync(reader);

            after.Should().ContainSingle(u => u.GroupId == groupId)
                .Which.Unread.Should().Be(1);
        }

        [Fact]
        public async Task TheReadCursorNeverMovesBackwards()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var author = members[0];
            var reader = members[1];

            await CreateExpenseAsync(author, groupId, 90m, members);

            var feed = await ActivityAsync(reader, groupId);
            var newest = feed.Max(e => e.Sequence);

            await MarkSeenAsync(reader, groupId, newest);
            await MarkSeenAsync(reader, groupId, 0);

            var after = await UnreadAsync(reader);

            after.Should().NotContain(u => u.GroupId == groupId);
        }

        [Fact]
        public async Task MarkingActivitySeenOnAGroupYouAreNotIn_Returns404()
        {
            var (groupId, _) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var response = await MarkSeenAsync(outsider, groupId, 1);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task TheFirstPairRequestCreates_TheSecondOneOnlyFinds()
        {
            var alice = await _fixture.RegisterAsync();
            var bob = await _fixture.RegisterAsync();

            var first = await alice.Client.PostAsJsonAsync(
                "/api/v1/groups/pairs", new EnsurePairRequest(bob.Id, "TRY"));

            var second = await alice.Client.PostAsJsonAsync(
                "/api/v1/groups/pairs", new EnsurePairRequest(bob.Id, "TRY"));

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task AskingForAPairTwice_ReturnsTheSameGroup()
        {
            var alice = await _fixture.RegisterAsync();
            var bob = await _fixture.RegisterAsync();

            var first = await EnsurePairAsync(alice, bob);
            var second = await EnsurePairAsync(alice, bob);

            second.Should().Be(first);
        }

        [Fact]
        public async Task EitherSideAsking_LandsOnTheSamePair()
        {
            var alice = await _fixture.RegisterAsync();
            var bob = await _fixture.RegisterAsync();

            var fromAlice = await EnsurePairAsync(alice, bob);
            var fromBob = await EnsurePairAsync(bob, alice);

            fromBob.Should().Be(fromAlice);
        }

        [Fact]
        public async Task TheSameTwoPeopleInAnotherCurrency_GetASeparatePair()
        {
            var alice = await _fixture.RegisterAsync();
            var bob = await _fixture.RegisterAsync();

            var tryPair = await EnsurePairAsync(alice, bob);
            var eurPair = await EnsurePairAsync(alice, bob, "EUR");

            eurPair.Should().NotBe(tryPair);
        }

        [Fact]
        public async Task AnExpenseInAPair_SplitsAndSettlesLikeAnyOtherGroup()
        {
            var alice = await _fixture.RegisterAsync();
            var bob = await _fixture.RegisterAsync();

            var pairId = await EnsurePairAsync(alice, bob);

            await CreateExpenseAsync(alice, pairId, 90m, new[] { alice, bob });

            var balance = await BalanceAsync(bob, pairId);

            Net(balance, alice.Id).Should().Be(45m);
            Net(balance, bob.Id).Should().Be(-45m);

            await SettleAsync(bob, pairId, alice, 45m);

            var after = await BalanceAsync(bob, pairId);

            after.Positions.Should().OnlyContain(pos => pos.Net == 0m);
            after.Debts.Should().BeEmpty();
        }

        [Fact]
        public async Task APairCannotTakeAThirdMember()
        {
            var alice = await _fixture.RegisterAsync();
            var bob = await _fixture.RegisterAsync();
            var carol = await _fixture.RegisterAsync();

            var pairId = await EnsurePairAsync(alice, bob);

            var response = await alice.Client.PostAsJsonAsync(
                $"/api/v1/groups/{pairId}/members", new InviteToGroupRequest(carol.Id));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PairingWithYourself_IsRejected()
        {
            var alice = await _fixture.RegisterAsync();

            var response = await alice.Client.PostAsJsonAsync(
                "/api/v1/groups/pairs", new EnsurePairRequest(alice.Id, "TRY"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AnExpenseWithAPlaceholder_SplitsLikeAnyOtherMember()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var payer = members[0];

            var mehmet = await AddPlaceholderAsync(payer, groupId, "Mehmet");

            var response = await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, 90m, "Market", DateTimeOffset.UtcNow,
                new List<ExpenseParticipantRequest>
                {
                    new(payer.Id, null),
                    new(members[1].Id, null),
                    new(mehmet, null)
                }));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var balance = await BalanceAsync(payer, groupId);

            Net(balance, payer.Id).Should().Be(60m);
            Net(balance, mehmet).Should().Be(-30m);
            balance.Positions.Sum(pos => pos.Net).Should().Be(0m);
        }

        [Fact]
        public async Task ASecondPlaceholderWithTheSameName_IsRejected()
        {
            var (groupId, members) = await GroupOfAsync(2);

            await AddPlaceholderAsync(members[0], groupId, "Mehmet");

            var response = await members[0].Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/placeholders", new AddPlaceholderRequest("  mehmet "));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SettlingOnBehalfOfAPlaceholder_ClearsTheirDebt()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var payer = members[0];

            var mehmet = await AddPlaceholderAsync(payer, groupId, "Mehmet");

            await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, 60m, "Market", DateTimeOffset.UtcNow,
                new List<ExpenseParticipantRequest> { new(payer.Id, null), new(mehmet, null) }));

            var settle = await SettleAsync(payer, groupId, payer, 30m, onBehalfOf: mehmet);
            settle.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var after = await BalanceAsync(payer, groupId);

            Net(after, mehmet).Should().Be(0m);
            Net(after, payer.Id).Should().Be(0m);
        }

        [Fact]
        public async Task SettlingOnBehalfOfARealUser_IsRejected()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];
            var debtor = members[1];

            await CreateExpenseAsync(payer, groupId, 90m, members);

            var response = await SettleAsync(payer, groupId, payer, 30m, onBehalfOf: debtor.Id);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var after = await BalanceAsync(payer, groupId);
            Net(after, debtor.Id).Should().Be(-30m);
        }

        [Fact]
        public async Task ClaimingAPlaceholder_KeepsTheHistory_AndLetsThemSignIn()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var payer = members[0];

            var mehmet = await AddPlaceholderAsync(payer, groupId, "Mehmet");

            await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, 60m, "Market", DateTimeOffset.UtcNow,
                new List<ExpenseParticipantRequest> { new(payer.Id, null), new(mehmet, null) }));

            var token = await IssueClaimTokenAsync(payer, mehmet);

            var username = $"u{Guid.NewGuid():N}"[..20];
            var claimed = await ClaimAsync(token, username, $"{username}@test.com", "password123");

            claimed.Should().Be(mehmet);

            var client = _fixture.CreateClient();
            var login = await client.PostAsJsonAsync(
                "/api/v1/auth/login", new LoginRequest(username, "password123"));

            login.StatusCode.Should().Be(HttpStatusCode.OK);

            var balance = await BalanceAsync(payer, groupId);

            Net(balance, mehmet).Should().Be(-30m);
            Net(balance, payer.Id).Should().Be(30m);
        }

        [Fact]
        public async Task AClaimTokenWorksOnlyOnce()
        {
            var (groupId, members) = await GroupOfAsync(2);

            var mehmet = await AddPlaceholderAsync(members[0], groupId, "Mehmet");
            var token = await IssueClaimTokenAsync(members[0], mehmet);

            var first = $"u{Guid.NewGuid():N}"[..20];
            await ClaimAsync(token, first, $"{first}@test.com", "password123");

            var second = $"u{Guid.NewGuid():N}"[..20];
            var response = await _fixture.CreateClient().PostAsJsonAsync(
                "/api/v1/placeholders/claim",
                new ClaimPlaceholderRequest(token, second, $"{second}@test.com", "password123"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task TheExpenseList_ShowsWhatYouOweOnEachLine()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];
            var reader = members[1];

            var expenseId = await CreateExpenseAsync(payer, groupId, 90m, members);

            var list = await ExpensesAsync(reader, groupId);

            var line = list.Should().ContainSingle().Subject;

            line.Id.Should().Be(expenseId);
            line.Amount.Should().Be(90m);
            line.Currency.Should().Be("TRY");
            line.PayerId.Should().Be(payer.Id);
            line.MyShare.Should().Be(30m);
            line.IsReversed.Should().BeFalse();
            line.Splits.Should().HaveCount(3);
        }

        [Fact]
        public async Task AReversedExpense_DropsOutOfTheListUnlessYouAskForIt()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var kept = await CreateExpenseAsync(payer, groupId, 60m, members);
            var cancelled = await CreateExpenseAsync(payer, groupId, 90m, members);

            await ReverseAsync(payer, cancelled, "wrong amount");

            var visible = await ExpensesAsync(payer, groupId);
            visible.Should().ContainSingle().Which.Id.Should().Be(kept);

            var everything = await ExpensesAsync(payer, groupId, includeReversed: true);
            everything.Should().HaveCount(2);

            var reversed = everything.Single(e => e.Id == cancelled);
            reversed.IsReversed.Should().BeTrue();
            reversed.ReversedBy.Should().Be(payer.Id);
            reversed.ReversalReason.Should().Be("wrong amount");
        }

        [Fact]
        public async Task ARevision_ShowsAsANewLineThatPointsAtTheOldOne()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var payer = members[0];

            var original = await CreateExpenseAsync(payer, groupId, 90m, members);

            var response = await ReviseAsync(payer, original, new ReviseExpenseRequest(
                60m, "Market", DateTimeOffset.UtcNow, Participants(members)));

            var revisionId = (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

            var visible = await ExpensesAsync(payer, groupId);

            var line = visible.Should().ContainSingle().Subject;
            line.Id.Should().Be(revisionId);
            line.Amount.Should().Be(60m);
            line.ReplacesExpenseId.Should().Be(original);
        }

        [Fact]
        public async Task TheExpenseListIsPaged_NewestFirst()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var payer = members[0];

            for (var i = 1; i <= 3; i++)
            {
                await PostExpenseAsync(payer, new CreateExpenseRequest(
                    groupId, payer.Id, i * 10m, $"Market {i}",
                    new DateTimeOffset(2026, 9, i, 0, 0, 0, TimeSpan.Zero), Participants(members)));
            }

            var firstPage = await ExpensesAsync(payer, groupId, take: 2);

            firstPage.Should().HaveCount(2);
            firstPage[0].Description.Should().Be("Market 3");
            firstPage[1].Description.Should().Be("Market 2");

            var secondPage = await ExpensesAsync(payer, groupId, skip: 2, take: 2);

            secondPage.Should().ContainSingle()
                .Which.Description.Should().Be("Market 1");
        }

        [Fact]
        public async Task TheExpenseListOfAGroupYouAreNotIn_Returns404()
        {
            var (groupId, _) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var response = await outsider.Client.GetAsync($"/api/v1/groups/{groupId}/expenses");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AnInviteLink_LetsSomeoneOutsideTheGroupJoin()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var link = await IssueInviteLinkAsync(members[0], groupId);

            link.Kind.Should().Be("Invite");
            link.Token.Should().StartWith("wli_");
            link.MaxUses.Should().Be(1);

            var joined = await JoinByLinkAsync(outsider, link.Token);
            joined.StatusCode.Should().Be(HttpStatusCode.OK);

            var group = (await joined.Content.ReadFromJsonAsync<GroupResponse>())!;

            group.Id.Should().Be(groupId);
            group.Members.Should().ContainSingle(m => m.UserId == outsider.Id && m.Status == "Active");

            (await GroupsAsync(outsider)).Should().Contain(g => g.Id == groupId);
        }

        [Fact]
        public async Task TheDefaultInviteLink_IsSingleUse()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var first = await _fixture.RegisterAsync();
            var second = await _fixture.RegisterAsync();

            var link = await IssueInviteLinkAsync(members[0], groupId);

            (await JoinByLinkAsync(first, link.Token)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await JoinByLinkAsync(second, link.Token)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var group = (await GroupsAsync(members[0])).Single(g => g.Id == groupId);

            group.Members.Should().NotContain(m => m.UserId == second.Id);
        }

        [Fact]
        public async Task AnInviteLinkWithThreeUses_AdmitsExactlyThree()
        {
            var (groupId, members) = await GroupOfAsync(2);

            var newcomers = new[]
            {
                await _fixture.RegisterAsync(),
                await _fixture.RegisterAsync(),
                await _fixture.RegisterAsync()
            };

            var late = await _fixture.RegisterAsync();

            var link = await IssueInviteLinkAsync(members[0], groupId, maxUses: 3);

            link.MaxUses.Should().Be(3);

            foreach (var newcomer in newcomers)
                (await JoinByLinkAsync(newcomer, link.Token)).StatusCode.Should().Be(HttpStatusCode.OK);

            (await JoinByLinkAsync(late, link.Token)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var group = (await GroupsAsync(members[0])).Single(g => g.Id == groupId);

            group.Members.Should().HaveCount(5);
        }

        [Fact]
        public async Task AnExistingMemberFollowingTheLink_DoesNotBurnTheInvite()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var invitee = await _fixture.RegisterAsync();

            var link = await IssueInviteLinkAsync(members[0], groupId);

            (await JoinByLinkAsync(members[1], link.Token)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await JoinByLinkAsync(invitee, link.Token)).StatusCode.Should().Be(HttpStatusCode.OK);

            var group = (await GroupsAsync(members[0])).Single(g => g.Id == groupId);

            group.Members.Should().ContainSingle(m => m.UserId == invitee.Id);
        }

        [Fact]
        public async Task FollowingTheSameLinkTwice_JoinsOnce_AndCostsOneUse()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var invitee = await _fixture.RegisterAsync();
            var other = await _fixture.RegisterAsync();

            var link = await IssueInviteLinkAsync(members[0], groupId, maxUses: 2);

            (await JoinByLinkAsync(invitee, link.Token)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await JoinByLinkAsync(invitee, link.Token)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await JoinByLinkAsync(other, link.Token)).StatusCode.Should().Be(HttpStatusCode.OK);

            var group = (await GroupsAsync(members[0])).Single(g => g.Id == groupId);

            group.Members.Count(m => m.UserId == invitee.Id).Should().Be(1);
            group.Members.Should().HaveCount(4);
        }

        [Fact]
        public async Task ARevokedInviteLink_StopsWorking()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var link = await IssueInviteLinkAsync(members[0], groupId);

            var revoked = await members[0].Client.DeleteAsync(
                $"/api/v1/groups/{groupId}/invite-links/{link.Id}");

            revoked.StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await JoinByLinkAsync(outsider, link.Token)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RevokingAnInviteLinkOfAGroupYouAreNotIn_Returns404()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var link = await IssueInviteLinkAsync(members[0], groupId);

            var response = await outsider.Client.DeleteAsync(
                $"/api/v1/groups/{groupId}/invite-links/{link.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AnUnknownToken_IsRejectedTheSameWayAsARevokedOne()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var link = await IssueInviteLinkAsync(members[0], groupId);

            await members[0].Client.DeleteAsync($"/api/v1/groups/{groupId}/invite-links/{link.Id}");

            var revoked = await JoinByLinkAsync(outsider, link.Token);
            var unknown = await JoinByLinkAsync(outsider, "wli_" + new string('a', 43));

            revoked.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            (await revoked.Content.ReadAsStringAsync())
                .Should().Be(await unknown.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task APairCannotIssueAnInviteLink()
        {
            var ayse = await _fixture.RegisterAsync();
            var burak = await _fixture.RegisterAsync();

            var pairId = await EnsurePairAsync(ayse, burak);

            var response = await ayse.Client.PostAsJsonAsync(
                $"/api/v1/groups/{pairId}/invite-links", new IssueInviteLinkRequest(null, null));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task IssuingAnInviteLinkForAGroupYouAreNotIn_Returns404()
        {
            var (groupId, _) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var response = await outsider.Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/invite-links", new IssueInviteLinkRequest(null, null));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AClaimTokenCannotBeIssuedForSomeoneWithARealAccount()
        {
            var (groupId, members) = await GroupOfAsync(2);

            var response = await members[0].Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/invite-links",
                new IssueInviteLinkRequest(members[1].Id, null));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AClaimTokenCannotAskForExtraUses()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var mehmet = await AddPlaceholderAsync(members[0], groupId, "Mehmet");

            var response = await members[0].Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/invite-links", new IssueInviteLinkRequest(mehmet, 5));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AClaimTokenIssuedThroughTheInviteEndpoint_ClaimsThePlaceholder()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var mehmet = await AddPlaceholderAsync(members[0], groupId, "Mehmet");

            var issued = await IssueInviteLinkAsync(members[0], groupId, placeholderId: mehmet);

            issued.Kind.Should().Be("PlaceholderClaim");
            issued.Token.Should().StartWith("wpc_");
            issued.MaxUses.Should().Be(1);

            var username = $"u{Guid.NewGuid():N}"[..20];
            var claimed = await ClaimAsync(issued.Token, username, $"{username}@test.com", "password123");

            claimed.Should().Be(mehmet);
        }

        [Fact]
        public async Task IssuingASecondClaimToken_KillsTheFirstOne()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var mehmet = await AddPlaceholderAsync(members[0], groupId, "Mehmet");

            var first = await IssueInviteLinkAsync(members[0], groupId, placeholderId: mehmet);
            var second = await IssueInviteLinkAsync(members[0], groupId, placeholderId: mehmet);

            var username = $"u{Guid.NewGuid():N}"[..20];

            var withFirst = await _fixture.CreateClient().PostAsJsonAsync(
                "/api/v1/placeholders/claim",
                new ClaimPlaceholderRequest(first.Token, username, $"{username}@test.com", "password123"));

            withFirst.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            (await ClaimAsync(second.Token, username, $"{username}@test.com", "password123"))
                .Should().Be(mehmet);
        }

        [Fact]
        public async Task TheGroupPlaceholderList_ShowsOnlyPlaceholders_AndOnlyToMembers()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var mehmet = await AddPlaceholderAsync(members[0], groupId, "Mehmet");
            var outsider = await _fixture.RegisterAsync();

            var seenByMember = await PlaceholdersAsync(members[0], groupId);

            seenByMember.Should().ContainSingle();
            seenByMember[0].UserId.Should().Be(mehmet);
            seenByMember[0].DisplayName.Should().Be("Mehmet");

            (await PlaceholdersAsync(outsider, groupId)).Should().BeEmpty();
        }

        [Fact]
        public async Task LeavingAGroupYouOweNothingIn_TakesYouOutOfIt()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var leaver = members[2];

            var response = await RemoveMemberAsync(leaver, groupId, leaver.Id);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await GroupsAsync(leaver)).Should().NotContain(g => g.Id == groupId);

            var seenByAdmin = (await GroupsAsync(members[0])).Single(g => g.Id == groupId);

            seenByAdmin.Members.Single(m => m.UserId == leaver.Id).Status.Should().Be("Removed");
        }

        [Fact]
        public async Task LeavingWithAnOpenBalance_IsRefused()
        {
            var (groupId, members) = await GroupOfAsync(2);

            await CreateExpenseAsync(members[0], groupId, 60m, members);

            Net(await BalanceAsync(members[0], groupId), members[1].Id).Should().Be(-30m);

            var response = await RemoveMemberAsync(members[1], groupId, members[1].Id);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            (await GroupsAsync(members[1])).Should().Contain(g => g.Id == groupId);
        }

        [Fact]
        public async Task LeavingWithOffsettingDebts_IsRefusedEvenThoughTheNetIsZero()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var middle = members[1];

            await CreateExpenseAsync(members[0], groupId, 100m, new[] { members[0], middle });
            await CreateExpenseAsync(middle, groupId, 100m, new[] { middle, members[2] });

            Net(await BalanceAsync(members[0], groupId), middle.Id).Should().Be(0m);

            var response = await RemoveMemberAsync(middle, groupId, middle.Id);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task APlainMemberCannotRemoveAnother()
        {
            var (groupId, members) = await GroupOfAsync(3);

            var response = await RemoveMemberAsync(members[1], groupId, members[2].Id);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            (await GroupsAsync(members[2])).Should().Contain(g => g.Id == groupId);
        }

        [Fact]
        public async Task TheAdminCanRemoveAMemberWhoOwesNothing()
        {
            var (groupId, members) = await GroupOfAsync(3);

            var response = await RemoveMemberAsync(members[0], groupId, members[2].Id);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await GroupsAsync(members[2])).Should().NotContain(g => g.Id == groupId);
        }

        [Fact]
        public async Task ARemovedMemberCannotRejoinWithAnInviteLink()
        {
            var (groupId, members) = await GroupOfAsync(3);
            var removed = members[2];

            (await RemoveMemberAsync(members[0], groupId, removed.Id))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);

            var link = await IssueInviteLinkAsync(members[0], groupId);

            (await JoinByLinkAsync(removed, link.Token)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            (await GroupsAsync(removed)).Should().NotContain(g => g.Id == groupId);
        }

        [Fact]
        public async Task RemovingAMemberOfAGroupYouAreNotIn_Returns404()
        {
            var (groupId, members) = await GroupOfAsync(2);
            var outsider = await _fixture.RegisterAsync();

            var response = await RemoveMemberAsync(outsider, groupId, members[1].Id);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private static async Task<HttpResponseMessage> RemoveMemberAsync(
            TestUser actor, Guid groupId, Guid userId) =>
            await actor.Client.DeleteAsync($"/api/v1/groups/{groupId}/members/{userId}");

        private static async Task<IssuedTokenResponse> IssueInviteLinkAsync(
            TestUser member, Guid groupId, Guid? placeholderId = null, int? maxUses = null)
        {
            var response = await member.Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/invite-links",
                new IssueInviteLinkRequest(placeholderId, maxUses));

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return (await response.Content.ReadFromJsonAsync<IssuedTokenResponse>())!;
        }

        private static async Task<HttpResponseMessage> JoinByLinkAsync(TestUser user, string token) =>
            await user.Client.PostAsJsonAsync(
                "/api/v1/groups/invite-links/join", new JoinGroupRequest(token));

        private static async Task<List<GroupResponse>> GroupsAsync(TestUser user) =>
            (await user.Client.GetFromJsonAsync<List<GroupResponse>>("/api/v1/groups"))!;

        private static async Task<List<GroupPlaceholderResponse>> PlaceholdersAsync(
            TestUser user, Guid groupId) =>
            (await user.Client.GetFromJsonAsync<List<GroupPlaceholderResponse>>(
                $"/api/v1/groups/{groupId}/placeholders"))!;

        private static async Task<Guid> CreateGroupAsync(TestUser owner, string currency = "TRY")
        {
            var created = await owner.Client.PostAsJsonAsync(
                "/api/v1/groups", new CreateGroupRequest("Piknik", currency));
            created.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        }

        private static async Task JoinAsync(TestUser inviter, Guid groupId, TestUser invitee)
        {
            var invite = await inviter.Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/members", new InviteToGroupRequest(invitee.Id));
            invite.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var invitations = await invitee.Client.GetFromJsonAsync<List<InvitationResponse>>("/api/v1/invitations");
            var invitation = invitations!.Single(i => i.GroupId == groupId);

            var accept = await invitee.Client.PostAsync($"/api/v1/invitations/{invitation.Id}/accept", null);
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
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/expenses")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());

            return await sender.Client.SendAsync(message);
        }

        private static async Task<List<ExpenseResponse>> ExpensesAsync(
            TestUser user, Guid groupId, bool includeReversed = false, int skip = 0, int take = 0)
        {
            var url = $"/api/v1/groups/{groupId}/expenses?includeReversed={includeReversed}";

            if (skip > 0)
                url += $"&skip={skip}";

            if (take > 0)
                url += $"&take={take}";

            var expenses = await user.Client.GetFromJsonAsync<List<ExpenseResponse>>(url);

            return expenses!;
        }

        private static async Task<Guid> AddPlaceholderAsync(
            TestUser member, Guid groupId, string displayName)
        {
            var response = await member.Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/placeholders", new AddPlaceholderRequest(displayName));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        }

        private static async Task<string> IssueClaimTokenAsync(TestUser member, Guid placeholderId)
        {
            var response = await member.Client.PostAsync(
                $"/api/v1/placeholders/{placeholderId}/claim-token", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return (await response.Content.ReadFromJsonAsync<ClaimTokenResponse>())!.Token;
        }

        private async Task<Guid> ClaimAsync(string token, string username, string email, string password)
        {
            var response = await _fixture.CreateClient().PostAsJsonAsync(
                "/api/v1/placeholders/claim",
                new ClaimPlaceholderRequest(token, username, email, password));

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        }

        private static async Task<Guid> EnsurePairAsync(
            TestUser caller, TestUser other, string currency = "TRY")
        {
            var response = await caller.Client.PostAsJsonAsync(
                "/api/v1/groups/pairs", new EnsurePairRequest(other.Id, currency));

            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

            return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        }

        private static async Task<int> UnreadCountAsync(TestUser user, Guid groupId)
        {
            var counts = await UnreadAsync(user);

            return counts.SingleOrDefault(c => c.GroupId == groupId)?.Unread ?? 0;
        }

        private static async Task<List<GroupUnreadCountResponse>> UnreadAsync(TestUser user)
        {
            var counts = await user.Client.GetFromJsonAsync<List<GroupUnreadCountResponse>>(
                "/api/v1/groups/activity/unread");

            return counts!;
        }

        private static async Task<HttpResponseMessage> MarkSeenAsync(
            TestUser user, Guid groupId, long sequence) =>
            await user.Client.PostAsJsonAsync(
                $"/api/v1/groups/{groupId}/activity/seen", new MarkActivitySeenRequest(sequence));

        private static async Task<List<ActivityEntryResponse>> ActivityAsync(
            TestUser user, Guid groupId, long? after = null)
        {
            var url = $"/api/v1/groups/{groupId}/activity";

            if (after is not null)
                url += $"?after={after}";

            var feed = await user.Client.GetFromJsonAsync<List<ActivityEntryResponse>>(url);

            return feed!;
        }

        private static async Task<Guid> CreateExpenseAsync(
            TestUser payer, Guid groupId, decimal amount, IEnumerable<TestUser> participants)
        {
            var created = await PostExpenseAsync(payer, new CreateExpenseRequest(
                groupId, payer.Id, amount, "Market", DateTimeOffset.UtcNow,
                Participants(participants.ToArray())));

            created.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        }

        private static async Task<HttpResponseMessage> ReverseAsync(
            TestUser actor, Guid expenseId, string? reason = null, string? idempotencyKey = null)
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Post, $"/api/v1/expenses/{expenseId}/reversal")
            {
                Content = JsonContent.Create(new ReverseExpenseRequest(reason))
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());

            return await actor.Client.SendAsync(message);
        }

        private static async Task<HttpResponseMessage> ReviseAsync(
            TestUser actor, Guid expenseId, ReviseExpenseRequest request, string? idempotencyKey = null)
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Post, $"/api/v1/expenses/{expenseId}/revisions")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());

            return await actor.Client.SendAsync(message);
        }

        private static async Task<int> LedgerEntryCountAsync(TestUser user)
        {
            var accounts = await user.Client.GetFromJsonAsync<List<AccountListResponse>>("/api/v1/accounts");
            var total = 0;

            foreach (var account in accounts!)
            {
                var entries = await user.Client.GetFromJsonAsync<List<LedgerEntryResponse>>(
                    $"/api/v1/accounts/{account.Id}/entries?take=200");

                total += entries!.Count;
            }

            return total;
        }

        private static async Task<HttpResponseMessage> SettleAsync(
            TestUser payer, Guid groupId, TestUser payee, decimal amount,
            string? idempotencyKey = null, Guid? onBehalfOf = null)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers")
            {
                Content = JsonContent.Create(
                    new TransferRequest(groupId, payee.Id, amount, onBehalfOf))
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());

            return await payer.Client.SendAsync(message);
        }

        private static async Task<GroupBalanceResponse> BalanceAsync(
            TestUser user, Guid groupId, bool simplify = false)
        {
            var balance = await user.Client.GetFromJsonAsync<GroupBalanceResponse>(
                $"/api/v1/groups/{groupId}/balance?simplify={(simplify ? "true" : "false")}");

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