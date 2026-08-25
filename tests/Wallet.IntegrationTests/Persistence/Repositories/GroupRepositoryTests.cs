using FluentAssertions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Domain.Groups;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories.GroupRepository;
using Wallet.Infrastructure.Persistence.Repositories.UserRepository;
using Wallet.Domain.Users;

namespace Wallet.IntegrationTests.Persistence.Repositories
{
    public class GroupRepositoryTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;
        public GroupRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

        private async Task AddAsync(Group group)
        {
            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            await new GroupRepository(context).AddAsync(group);
            await new UnitOfWork(context).SaveChangesAsync();
        }

        [Fact]
        public async Task NamedGroup_RoundTripsWithItsMembers()
        {
            var id = Guid.NewGuid();
            var creator = Guid.NewGuid();
            var invited = Guid.NewGuid();

            var group = Group.CreateNamedGroup(id, "Piknik", "USD", creator);
            group.Invite(invited, creator);
            await AddAsync(group);

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var loaded = await new GroupRepository(context).GetByIdAsync(id);

            loaded.Should().NotBeNull();
            loaded!.Kind.Should().Be(GroupKind.Named);
            loaded.Name.Should().Be("Piknik");
            loaded.Currency.Should().Be("USD");
            loaded.PairKey.Should().BeNull();
            loaded.Members.Should().HaveCount(2);
            loaded.IsActiveMember(creator).Should().BeTrue();
            loaded.IsActiveMember(invited).Should().BeFalse();
        }

        [Fact]
        public async Task InvitedMember_IsPersisted_WhenAddedToATrackedGroup()
        {
            var id = Guid.NewGuid();
            var creator = Guid.NewGuid();
            var invited = Guid.NewGuid();

            await AddAsync(Group.CreateNamedGroup(id, "Piknik", "USD", creator));

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var repository = new GroupRepository(context);
                var group = await repository.GetByIdAsync(id);
                group!.Invite(invited, creator);
                await new UnitOfWork(context).SaveChangesAsync();
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.System))
            {
                var loaded = await new GroupRepository(context).GetByIdAsync(id);
                loaded!.Members.Should().HaveCount(2);
                loaded.Members.Should().Contain(m => m.UserId == invited && m.Status == GroupMemberStatus.Invited);
            }
        }

        [Fact]
        public async Task SecondPair_ForTheSameTwoPeopleAndCurrency_IsRejected()
        {
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            await AddAsync(Group.CreatePair(Guid.NewGuid(), "USD", userA, userB));

            var act = async () => await AddAsync(Group.CreatePair(Guid.NewGuid(), "USD", userB, userA));

            await act.Should().ThrowAsync<UniqueConstraintViolationException>();
        }

        [Fact]
        public async Task SamePair_InADifferentCurrency_IsAllowed()
        {
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            await AddAsync(Group.CreatePair(Guid.NewGuid(), "USD", userA, userB));

            var act = async () => await AddAsync(Group.CreatePair(Guid.NewGuid(), "EUR", userA, userB));

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task NamedGroups_DoNotCollide_OnNullPairKey()
        {
            await AddAsync(Group.CreateNamedGroup(Guid.NewGuid(), "A", "USD", Guid.NewGuid()));

            var act = async () => await AddAsync(Group.CreateNamedGroup(Guid.NewGuid(), "B", "USD", Guid.NewGuid()));

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GetPair_FindsTheGroup_RegardlessOfArgumentOrder()
        {
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();
            var id = Guid.NewGuid();

            await AddAsync(Group.CreatePair(id, "USD", userA, userB));

            await using var context = _fixture.CreateContext(TestCurrentUser.System);
            var found = await new GroupRepository(context).GetPairAsync(userB, userA, "usd");

            found.Should().NotBeNull();
            found!.Id.Should().Be(id);
        }

        [Fact]
        public async Task User_SeesOnlyGroupsTheyActivelyBelongTo()
        {
            var member = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            var invitedOnly = Guid.NewGuid();

            var mine = Guid.NewGuid();
            var group = Group.CreateNamedGroup(mine, "Mine", "USD", member);
            group.Invite(invitedOnly, member);
            await AddAsync(group);

            await AddAsync(Group.CreateNamedGroup(Guid.NewGuid(), "Theirs", "USD", stranger));

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(member)))
            {
                var groups = await new GroupRepository(context).GetAllAsync();
                groups.Should().ContainSingle(g => g.Id == mine);
            }

            await using (var context = _fixture.CreateContext(TestCurrentUser.For(invitedOnly)))
            {
                var groups = await new GroupRepository(context).GetAllAsync();
                groups.Should().BeEmpty();
            }
        }
    }
}
