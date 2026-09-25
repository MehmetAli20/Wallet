using FluentValidation;

namespace Wallet.Application.Activity.MarkActivitySeen
{
    public class MarkActivitySeenCommandValidator : AbstractValidator<MarkActivitySeenCommand>
    {
        public MarkActivitySeenCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.Sequence).GreaterThanOrEqualTo(0);
        }
    }
}
