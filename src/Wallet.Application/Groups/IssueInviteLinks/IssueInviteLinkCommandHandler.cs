using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;
using Wallet.Domain.Users;

namespace Wallet.Application.Groups.IssueInviteLinks
{
    public class IssueInviteLinkCommandHandler : IRequestHandler<IssueInviteLinkCommand, IssuedGroupToken>
    {
        private const int DefaultMaxUses = 1;

        private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

        private readonly IGroupRepository _groups;
        private readonly IGroupInviteLinkRepository _links;
        private readonly IPlaceholderClaimRepository _claims;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public IssueInviteLinkCommandHandler(
            IGroupRepository groups,
            IGroupInviteLinkRepository links,
            IPlaceholderClaimRepository claims,
            IUserRepository users,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _groups = groups;
            _links = links;
            _claims = claims;
            _users = users;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<IssuedGroupToken> Handle(
            IssueInviteLinkCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            var now = DateTimeOffset.UtcNow;

            if (request.PlaceholderUserId is Guid placeholderId)
            {
                if (!group.IsActiveMember(placeholderId))
                    throw new InvalidGroupOperationException("That member is not part of this group.");

                var placeholder = await _users.GetByIdAsync(placeholderId, cancellationToken)
                    ?? throw new InvalidGroupOperationException("That member is not part of this group.");

                if (!placeholder.IsPlaceholder)
                    throw new InvalidGroupOperationException("That member already has an account.");

                foreach (var outstanding in await _claims.GetOutstandingForPlaceholderAsync(
                    placeholderId, now, cancellationToken))
                {
                    outstanding.Revoke(now);
                }

                var issuedClaim = PlaceholderClaim.Issue(
                    Guid.NewGuid(), placeholderId, now.Add(PlaceholderClaim.Lifetime));

                await _claims.AddAsync(issuedClaim.Claim, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new IssuedGroupToken(
                    GroupTokenKind.PlaceholderClaim,
                    issuedClaim.Claim.Id,
                    issuedClaim.Token,
                    issuedClaim.Claim.ExpiresAt,
                    1);
            }

            var issuedLink = GroupInviteLink.Issue(
                Guid.NewGuid(),
                group,
                _currentUser.UserId,
                now.Add(InviteLifetime),
                request.MaxUses ?? DefaultMaxUses);

            await _links.AddAsync(issuedLink.Link, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new IssuedGroupToken(
                GroupTokenKind.Invite,
                issuedLink.Link.Id,
                issuedLink.Token,
                issuedLink.Link.ExpiresAt,
                issuedLink.Link.MaxUses);
        }
    }
}
