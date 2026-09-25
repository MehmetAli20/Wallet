using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Users;

namespace Wallet.Application.Groups.IssueClaimToken
{
    public class IssueClaimTokenCommandHandler : IRequestHandler<IssueClaimTokenCommand, string>
    {
        private readonly IUserRepository _users;
        private readonly IPlaceholderClaimRepository _claims;
        private readonly IUnitOfWork _unitOfWork;

        public IssueClaimTokenCommandHandler(
            IUserRepository users,
            IPlaceholderClaimRepository claims,
            IUnitOfWork unitOfWork)
        {
            _users = users;
            _claims = claims;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> Handle(IssueClaimTokenCommand request, CancellationToken cancellationToken)
        {
            var placeholder = await _users.GetPlaceholderInMyGroupsAsync(
                request.PlaceholderUserId, cancellationToken)
                ?? throw new InvalidGroupOperationException("Placeholder not found.");

            if (!placeholder.IsPlaceholder)
                throw new InvalidGroupOperationException("This member already has an account.");

            var now = DateTimeOffset.UtcNow;

            foreach (var outstanding in await _claims.GetOutstandingForPlaceholderAsync(
                placeholder.Id, now, cancellationToken))
            {
                outstanding.Revoke(now);
            }

            var issued = PlaceholderClaim.Issue(
                Guid.NewGuid(), placeholder.Id, now.Add(PlaceholderClaim.Lifetime));

            await _claims.AddAsync(issued.Claim, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return issued.Token;
        }
    }
}
