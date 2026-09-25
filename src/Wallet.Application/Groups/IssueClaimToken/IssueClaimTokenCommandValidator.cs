using FluentValidation;

namespace Wallet.Application.Groups.IssueClaimToken
{
    public class IssueClaimTokenCommandValidator : AbstractValidator<IssueClaimTokenCommand>
    {
        public IssueClaimTokenCommandValidator()
        {
            RuleFor(x => x.PlaceholderUserId).NotEmpty();
        }
    }
}
