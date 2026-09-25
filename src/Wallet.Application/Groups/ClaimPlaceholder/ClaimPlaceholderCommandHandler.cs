using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Groups.ClaimPlaceholder
{
    public class ClaimPlaceholderCommandHandler : IRequestHandler<ClaimPlaceholderCommand, Guid>
    {
        private readonly IPlaceholderClaimRepository _claims;
        private readonly IUserRepository _users;
        private readonly IGroupRepository _groups;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public ClaimPlaceholderCommandHandler(
            IPlaceholderClaimRepository claims,
            IUserRepository users,
            IGroupRepository groups,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _claims = claims;
            _users = users;
            _groups = groups;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(ClaimPlaceholderCommand request, CancellationToken cancellationToken)
        {
            var claim = await _claims.GetUsableAsync(request.Token, DateTimeOffset.UtcNow, cancellationToken)
                ?? throw new InvalidGroupOperationException("This claim link is not valid any more.");

            var placeholder = await _users.GetByIdAsync(claim.PlaceholderUserId, cancellationToken)
                ?? throw new InvalidGroupOperationException("Placeholder not found.");

            if (await _users.GetByUsernameAsync(request.Username, cancellationToken) is not null)
                throw new UsernameAlreadyExistsException();

            if (await _users.GetByEmailAsync(request.Email, cancellationToken) is not null)
                throw new EmailAlreadyExistsException();

            placeholder.Promote(
                request.Username, request.Email, _passwordHasher.Hash(request.Password));

            claim.Use(DateTimeOffset.UtcNow);

            foreach (var group in await _groups.GetAllForPartyAsync(placeholder.Id, cancellationToken))
                group.RecordPlaceholderClaimed(placeholder.Id);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return placeholder.Id;
        }
    }
}
