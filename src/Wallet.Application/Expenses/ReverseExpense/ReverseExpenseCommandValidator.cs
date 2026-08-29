using FluentValidation;

namespace Wallet.Application.Expenses.ReverseExpense
{
    public class ReverseExpenseCommandValidator : AbstractValidator<ReverseExpenseCommand>
    {
        public ReverseExpenseCommandValidator()
        {
            RuleFor(x => x.ExpenseId).NotEmpty();
            RuleFor(x => x.Reason).MaximumLength(200);
            RuleFor(x => x.IdempotencyKey).NotEmpty();
        }
    }
}
