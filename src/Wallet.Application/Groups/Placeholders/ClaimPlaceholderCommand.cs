using FluentValidation;
using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Users;

namespace Wallet.Application.Groups.Placeholders
{
    public record IssueClaimTokenCommand(Guid PlaceholderUserId) : IRequest<string>;

    public record ClaimPlaceholderCommand(
        string Token,
        string Username,
        string Email,
        string Password) : IRequest<Guid>;

    public class IssueClaimTokenCommandValidator : AbstractValidator<IssueClaimTokenCommand>
    {
        public IssueClaimTokenCommandValidator()
        {
            RuleFor(x => x.PlaceholderUserId).NotEmpty();
        }
    }

    public class ClaimPlaceholderCommandValidator : AbstractValidator<ClaimPlaceholderCommand>
    {
        public ClaimPlaceholderCommandValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50)
                .Matches("^[a-zA-Z0-9._-]+$");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(72);
        }
    }

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

            var claim = PlaceholderClaim.Issue(
                Guid.NewGuid(), placeholder.Id, DateTimeOffset.UtcNow.AddDays(14));

            await _claims.AddAsync(claim, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return claim.Token;
        }
    }

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
