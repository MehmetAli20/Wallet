using FluentValidation;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.ReportUser
{
    public class ReportUserCommandValidator : AbstractValidator<ReportUserCommand>
    {
        public ReportUserCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Reason).IsInEnum();
            RuleFor(x => x.Details).MaximumLength(UserReport.DetailsMaxLength);
        }
    }
}
