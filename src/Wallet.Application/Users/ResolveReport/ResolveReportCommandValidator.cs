using FluentValidation;

namespace Wallet.Application.Users.ResolveReport
{
    public class ResolveReportCommandValidator : AbstractValidator<ResolveReportCommand>
    {
        public ResolveReportCommandValidator()
        {
            RuleFor(x => x.ReportId).NotEmpty();
        }
    }
}
