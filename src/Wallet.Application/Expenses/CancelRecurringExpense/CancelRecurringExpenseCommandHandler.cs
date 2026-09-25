using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Expenses.CancelRecurringExpense
{
    public class CancelRecurringExpenseCommandHandler : IRequestHandler<CancelRecurringExpenseCommand>
    {
        private readonly IRecurringExpenseRepository _recurring;
        private readonly IUnitOfWork _unitOfWork;

        public CancelRecurringExpenseCommandHandler(
            IRecurringExpenseRepository recurring, IUnitOfWork unitOfWork)
        {
            _recurring = recurring;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            CancelRecurringExpenseCommand request, CancellationToken cancellationToken)
        {
            var recurring = await _recurring.GetByIdAsync(request.RecurringExpenseId, cancellationToken)
                ?? throw new ExpenseNotFoundException(request.RecurringExpenseId);

            recurring.Cancel();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
