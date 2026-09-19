using FluentValidation;

namespace Wallet.Application.Groups.JoinGroupByLink
{
    public class JoinGroupByLinkCommandValidator : AbstractValidator<JoinGroupByLinkCommand>
    {
        public JoinGroupByLinkCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .Matches("^wli_[A-Za-z0-9_-]{43}$");
        }
    }
}
