using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Exceptions;

namespace Wallet.Domain.Groups
{
    public class GroupMember
    {
        public Guid Id { get; private set; }
        public Guid GroupId { get; private set; }
        public Guid UserId { get; private set; }
        public GroupMemberRole Role { get; private set; }
        public GroupMemberStatus Status { get; private set; }
        public Guid? InvitedBy { get; private set; }
        public DateTimeOffset InvitedAt { get; private set; }
        public DateTimeOffset? JoinedAt { get; private set; }
        
        private GroupMember()
        {

        }

        internal GroupMember(Guid id, Guid groupId, Guid userId, GroupMemberRole role, GroupMemberStatus status, Guid? invitedBy = null)
        {
            if(id == Guid.Empty)
            {
                throw new ArgumentException("Member Id cannot be empty.", nameof(id));
            }

            if(groupId == Guid.Empty)
            {
                throw new ArgumentException("Group Id cannot be empty.", nameof(groupId));
            }
            if(userId == Guid.Empty)
            {
                throw new ArgumentException("User Id cannot be empty.", nameof(userId));
            }

            Id = id;
            GroupId = groupId;
            UserId = userId;
            Role = role;
            Status = status;
            InvitedBy = invitedBy;
            InvitedAt = DateTimeOffset.UtcNow;
            JoinedAt = status == GroupMemberStatus.Active ? InvitedAt : null;
        }

        internal void Accept()
        {
            if(Status == GroupMemberStatus.Removed)
            {
                throw new InvalidGroupOperationException("A removed member needs a new invitation to return.");
            }

            if(Status == GroupMemberStatus.Active)
            {
                throw new InvalidGroupOperationException("Member has already joined.");
            }

            Status = GroupMemberStatus.Active;
            JoinedAt = DateTimeOffset.UtcNow;
        }

        internal void Remove()
        {
            if(Status == GroupMemberStatus.Removed)
            {
                throw new InvalidGroupOperationException("Member has already been removed.");
            }

            Status = GroupMemberStatus.Removed;
            JoinedAt = null;
        }

        internal void Reinvite(Guid invitedBy)
        {
            Status = GroupMemberStatus.Invited;
            Role = GroupMemberRole.Member;
            InvitedBy = invitedBy;
            InvitedAt = DateTimeOffset.UtcNow;
            JoinedAt = null;
        }
    }
}