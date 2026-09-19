using FluentAssertions;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.UnitTests.Domain.Groups
{
    public class GroupTests
    {
        private static Group NewNamed(Guid? creator = null) =>
            Group.CreateNamedGroup(Guid.NewGuid(), "Piknik", "USD", creator ?? Guid.NewGuid());

        [Fact]
        public void CreateNamedGroup_WithValidInput_SetsUpAdminMember()
        {
            var id = Guid.NewGuid();
            var creator = Guid.NewGuid();

            var group = Group.CreateNamedGroup(id, "  Piknik  ", "usd", creator);

            group.Id.Should().Be(id);
            group.Kind.Should().Be(GroupKind.Named);
            group.Name.Should().Be("Piknik");
            group.Currency.Should().Be("USD");
            group.PairKey.Should().BeNull();

            group.Members.Should().ContainSingle();
            group.Members[0].UserId.Should().Be(creator);
            group.Members[0].Role.Should().Be(GroupMemberRole.Admin);
            group.Members[0].Status.Should().Be(GroupMemberStatus.Active);
            group.Members[0].JoinedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateNamedGroup_WithEmptyName_Throws(string name)
        {
            var act = () => Group.CreateNamedGroup(Guid.NewGuid(), name, "USD", Guid.NewGuid());
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateNamedGroup_WithEmptyCreator_Throws()
        {
            var act = () => Group.CreateNamedGroup(Guid.NewGuid(), "Piknik", "USD", Guid.Empty);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateNamedGroup_WithEmptyId_Throws()
        {
            var act = () => Group.CreateNamedGroup(Guid.Empty, "Piknik", "USD", Guid.NewGuid());
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateNamedGroup_WithInvalidCurrency_Throws()
        {
            var act = () => Group.CreateNamedGroup(Guid.NewGuid(), "Piknik", "US", Guid.NewGuid());
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreatePair_WithValidInput_AddsBothMembersAsActive()
        {
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            var pair = Group.CreatePair(Guid.NewGuid(), "usd", userA, userB);

            pair.Kind.Should().Be(GroupKind.Pair);
            pair.Name.Should().BeNull();
            pair.Currency.Should().Be("USD");
            pair.PairKey.Should().NotBeNull();

            pair.Members.Should().HaveCount(2);
            pair.Members.Should().OnlyContain(m => m.Status == GroupMemberStatus.Active);
            pair.Members.Should().OnlyContain(m => m.Role == GroupMemberRole.Member);
            pair.Members.Select(m => m.UserId).Should().BeEquivalentTo(new[] { userA, userB });
        }

        [Fact]
        public void CreatePair_WithSameUserTwice_Throws()
        {
            var user = Guid.NewGuid();

            var act = () => Group.CreatePair(Guid.NewGuid(), "USD", user, user);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void CreatePair_WithEmptyUser_Throws()
        {
            var act = () => Group.CreatePair(Guid.NewGuid(), "USD", Guid.NewGuid(), Guid.Empty);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void BuildPairKey_IsIndependentOfArgumentOrder()
        {
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            Group.BuildPairKey(userA, userB).Should().Be(Group.BuildPairKey(userB, userA));
        }

        [Fact]
        public void Invite_AddsMemberAsInvited()
        {
            var group = NewNamed();
            var invited = Guid.NewGuid();

            var member = group.Invite(invited, group.Members[0].UserId);

            member.UserId.Should().Be(invited);
            member.Role.Should().Be(GroupMemberRole.Member);
            member.Status.Should().Be(GroupMemberStatus.Invited);
            member.JoinedAt.Should().BeNull();
            group.Members.Should().HaveCount(2);
        }

        [Fact]
        public void Invite_OnPair_Throws()
        {
            var pair = Group.CreatePair(Guid.NewGuid(), "USD", Guid.NewGuid(), Guid.NewGuid());

            var act = () => pair.Invite(Guid.NewGuid(), Guid.NewGuid());

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Invite_ExistingMember_Throws()
        {
            var creator = Guid.NewGuid();
            var group = NewNamed(creator);

            var act = () => group.Invite(creator, creator);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Invite_WithEmptyUserId_Throws()
        {
            var group = NewNamed();
            var act = () => group.Invite(Guid.Empty, group.Members[0].UserId);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Accept_MakesInvitedMemberActive()
        {
            var group = NewNamed();
            var invited = Guid.NewGuid();
            group.Invite(invited, group.Members[0].UserId);

            group.Accept(invited);

            var member = group.Members.Single(m => m.UserId == invited);
            member.Status.Should().Be(GroupMemberStatus.Active);
            member.JoinedAt.Should().NotBeNull();
        }

        [Fact]
        public void Accept_ByNonInvitedUser_Throws()
        {
            var group = NewNamed();

            var act = () => group.Accept(Guid.NewGuid());

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Accept_Twice_Throws()
        {
            var group = NewNamed();
            var invited = Guid.NewGuid();
            group.Invite(invited, group.Members[0].UserId);
            group.Accept(invited);

            var act = () => group.Accept(invited);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Remove_TakesMemberOutOfGroup()
        {
            var group = NewNamed();
            var invited = Guid.NewGuid();
            group.Invite(invited, group.Members[0].UserId);

            group.Remove(invited, group.Members[0].UserId);

            group.IsActiveMember(invited).Should().BeFalse();
            group.Members.Single(m => m.UserId == invited).Status
                .Should().Be(GroupMemberStatus.Removed);
        }

        [Fact]
        public void Remove_OnPair_Throws()
        {
            var userA = Guid.NewGuid();
            var pair = Group.CreatePair(Guid.NewGuid(), "USD", userA, Guid.NewGuid());

            var act = () => pair.Remove(userA, userA);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Remove_NonMember_Throws()
        {
            var group = NewNamed();

            var act = () => group.Remove(Guid.NewGuid(), group.Members[0].UserId);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Remove_UnknownUserFromPair_ReportsThePairRule()
        {
            var pair = Group.CreatePair(Guid.NewGuid(), "USD", Guid.NewGuid(), Guid.NewGuid());

            var act = () => pair.Remove(Guid.NewGuid(), Guid.NewGuid());

            act.Should().Throw<InvalidGroupOperationException>()
                .WithMessage("*pair*");
        }

        [Fact]
        public void Remove_LastAdmin_Throws()
        {
            var creator = Guid.NewGuid();
            var group = NewNamed(creator);
            var invited = Guid.NewGuid();
            group.Invite(invited, group.Members[0].UserId);
            group.Accept(invited);

            var act = () => group.Remove(creator, creator);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Remove_Twice_Throws()
        {
            var group = NewNamed();
            var admin = group.Members[0].UserId;
            var invited = Guid.NewGuid();

            group.Invite(invited, admin);
            group.Remove(invited, admin);

            var act = () => group.Remove(invited, admin);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void ARemovedMember_CannotComeBackThroughAnInviteLink()
        {
            var group = NewNamed();
            var admin = group.Members[0].UserId;
            var invited = Guid.NewGuid();

            group.Invite(invited, admin);
            group.Accept(invited);
            group.Remove(invited, admin);

            var act = () => group.Join(invited, admin);

            act.Should().Throw<InvalidGroupOperationException>();
            group.IsActiveMember(invited).Should().BeFalse();
        }

        [Fact]
        public void ARemovedMember_CannotAcceptTheirOldInvitation()
        {
            var group = NewNamed();
            var admin = group.Members[0].UserId;
            var invited = Guid.NewGuid();

            group.Invite(invited, admin);
            group.Accept(invited);
            group.Remove(invited, admin);

            var act = () => group.Accept(invited);

            act.Should().Throw<InvalidGroupOperationException>();
            group.IsActiveMember(invited).Should().BeFalse();
        }

        [Fact]
        public void ARemovedMember_ComesBackOnlyThroughAFreshInvitation()
        {
            var group = NewNamed();
            var admin = group.Members[0].UserId;
            var invited = Guid.NewGuid();

            group.Invite(invited, admin);
            group.Accept(invited);
            group.Remove(invited, admin);

            var reinvited = group.Invite(invited, admin);

            reinvited.Status.Should().Be(GroupMemberStatus.Invited);
            reinvited.Role.Should().Be(GroupMemberRole.Member);
            reinvited.JoinedAt.Should().BeNull();

            group.Members.Count(m => m.UserId == invited).Should().Be(1);

            group.Accept(invited);

            group.IsActiveMember(invited).Should().BeTrue();
        }


        [Fact]
        public void Invite_ByNonMember_Throws()
        {
            var group = NewNamed();

            var act = () => group.Invite(Guid.NewGuid(), Guid.NewGuid());

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Invite_ByInvitedButNotAcceptedMember_Throws()
        {
            var creator = Guid.NewGuid();
            var group = NewNamed(creator);
            var invited = Guid.NewGuid();
            group.Invite(invited, creator);

            var act = () => group.Invite(Guid.NewGuid(), invited);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void Invite_RecordsWhoInvited()
        {
            var creator = Guid.NewGuid();
            var group = NewNamed(creator);
            var invited = Guid.NewGuid();

            var member = group.Invite(invited, creator);

            member.InvitedBy.Should().Be(creator);
        }

        [Fact]
        public void Creator_HasNoInviter()
        {
            var group = NewNamed();

            group.Members[0].InvitedBy.Should().BeNull();
        }

        [Fact]
        public void DeclineInvitation_RemovesThePendingMember()
        {
            var creator = Guid.NewGuid();
            var group = NewNamed(creator);
            var invited = Guid.NewGuid();
            group.Invite(invited, creator);

            group.DeclineInvitation(invited);

            group.Members.Should().ContainSingle();
            group.Members.Should().NotContain(m => m.UserId == invited);
        }

        [Fact]
        public void DeclineInvitation_ByActiveMember_Throws()
        {
            var creator = Guid.NewGuid();
            var group = NewNamed(creator);

            var act = () => group.DeclineInvitation(creator);

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void DeclineInvitation_ByNonMember_Throws()
        {
            var group = NewNamed();

            var act = () => group.DeclineInvitation(Guid.NewGuid());

            act.Should().Throw<InvalidGroupOperationException>();
        }

        [Fact]
        public void IsActiveMember_IsFalseUntilInviteIsAccepted()
        {
            var group = NewNamed();
            var invited = Guid.NewGuid();
            group.Invite(invited, group.Members[0].UserId);

            group.IsActiveMember(invited).Should().BeFalse();

            group.Accept(invited);

            group.IsActiveMember(invited).Should().BeTrue();
        }

        [Fact]
        public void IsActiveMember_IsFalseForNonMember()
        {
            NewNamed().IsActiveMember(Guid.NewGuid()).Should().BeFalse();
        }
    }
}