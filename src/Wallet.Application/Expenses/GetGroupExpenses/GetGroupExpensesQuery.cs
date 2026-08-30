using MediatR;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Abstractions.Groups;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.GetGroupExpenses
{
    public record GetGroupExpensesQuery(Guid GroupId, bool IncludeReversed, int Skip, int Take)
        : IRequest<IReadOnlyList<Expense>>;

    public class GetGroupExpensesQueryHandler
        : IRequestHandler<GetGroupExpensesQuery, IReadOnlyList<Expense>>
    {
        private readonly IGroupRepository _groups;
        private readonly IExpenseRepository _expenses;

        public GetGroupExpensesQueryHandler(IGroupRepository groups, IExpenseRepository expenses)
        {
            _groups = groups;
            _expenses = expenses;
        }

        public async Task<IReadOnlyList<Expense>> Handle(
            GetGroupExpensesQuery request, CancellationToken cancellationToken)
        {
            _ = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            return await _expenses.GetByGroupAsync(
                request.GroupId,
                request.IncludeReversed,
                request.Skip,
                request.Take,
                cancellationToken);
        }
    }
}
