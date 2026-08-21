using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;

namespace Wallet.Domain.Groups
{
    public class Group
    {
        private readonly List<GroupMember> _members = new();
        
        public Guid Id { get; private set; }
        public GroupKind Kind { get; private set; }
        public string? Name { get; private set; }
        public string Currency {  get; private set; }
        public string? PairKey { get; private set; }
        public IReadOnlyList<GroupMember> Members => _members.AsReadOnly();

        private Group()
        {
            Currency = null!;
        }

        private Group(Guid id, GroupKind kind,  string? name, string currency, string? pairKey)
        {
            if(id == Guid.Empty)
            {
                throw new ArgumentException("Group Id cannot be empty.", nameof(id));
            }
            
            Id = id;
            Kind = kind;
            Name = name;
            Currency = Money.NormalizeCurrency(currency);
            PairKey = pairKey;
        }

        public static Group CreateNamedGroup(Guid id, string groupName, string currency, Guid creatorUserId)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Group name cannot be empty.", nameof(groupName));
            }

            if (creatorUserId == Guid.Empty)
            {
                throw new ArgumentException("Creator Id cannot be empty.", nameof(creatorUserId));
            }

            var group = new Group(id, GroupKind.Named, groupName.Trim(), currency, pairKey: null);

            group._members.Add(new GroupMember(Guid.NewGuid(), id, creatorUserId, GroupMemberRole.Admin, GroupMemberStatus.Active));

            return group;
        }

        public static Group CreatePair(Guid id, string currency, Guid userA, Guid userB)
        {
            if (userA == Guid.Empty || userB == Guid.Empty)
                throw new ArgumentException("Pair members cannot be empty.");

            if (userA == userB)
                throw new InvalidGroupOperationException("A pair needs two different people.");

            var group = new Group(id, GroupKind.Pair, name: null, currency, BuildPairKey(userA, userB));

            group._members.Add(new GroupMember(
                Guid.NewGuid(), id, userA, GroupMemberRole.Member, GroupMemberStatus.Active));
            group._members.Add(new GroupMember(
                Guid.NewGuid(), id, userB, GroupMemberRole.Member, GroupMemberStatus.Active));

            return group;
        }

        public static string BuildPairKey(Guid userA, Guid userB) =>
            userA.CompareTo(userB) < 0 ? $"{userA:N}:{userB:N}" : $"{userB:N}:{userA:N}";

        public GroupMember Invite(Guid userId)
        {
            if (Kind == GroupKind.Pair)
                throw new InvalidGroupOperationException("A pair cannot take a third member. Create a named group instead.");

            if (userId == Guid.Empty)
                throw new ArgumentException("User Id cannot be empty.", nameof(userId));

            if (_members.Any(m => m.UserId == userId))
                throw new InvalidGroupOperationException("User is already in the group.");

            var member = new GroupMember(
                Guid.NewGuid(), Id, userId, GroupMemberRole.Member, GroupMemberStatus.Invited);

            _members.Add(member);
            return member;
        }

        public void Accept(Guid userId)
        {
            var member = _members.SingleOrDefault(m => m.UserId == userId)
                ?? throw new InvalidGroupOperationException("User was not invited to this group.");

            member.Accept();
        }

        public void Remove(Guid userId)
        {
            if (Kind == GroupKind.Pair)
                throw new InvalidGroupOperationException("Members of a pair cannot be removed.");

            var member = _members.SingleOrDefault(m => m.UserId == userId)
                ?? throw new InvalidGroupOperationException("User is not in this group.");

            if (member.Role == GroupMemberRole.Admin && _members.Count(m => m.Role == GroupMemberRole.Admin) == 1)
                throw new InvalidGroupOperationException("The last admin cannot be removed.");

            _members.Remove(member);
        }

        public bool IsActiveMember(Guid userId) =>
            _members.Any(m => m.UserId == userId && m.Status == GroupMemberStatus.Active);
    }
}