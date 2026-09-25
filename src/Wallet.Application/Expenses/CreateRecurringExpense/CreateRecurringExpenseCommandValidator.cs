using FluentValidation;

namespace Wallet.Application.Expenses.CreateRecurringExpense
{
    public class CreateRecurringExpenseCommandValidator : AbstractValidator<CreateRecurringExpenseCommand>
    {
        public CreateRecurringExpenseCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.PayerId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Interval).IsInEnum();
            RuleFor(x => x.FirstOccurrence).NotEqual(default(DateTimeOffset));
            RuleFor(x => x.Participants).NotEmpty();
            RuleFor(x => x.IdempotencyKey).NotEmpty();
        }
    }
}
