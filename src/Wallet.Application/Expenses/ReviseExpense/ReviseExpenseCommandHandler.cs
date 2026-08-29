using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.ReviseExpense
{
    public class ReviseExpenseCommandHandler : IRequestHandler<ReviseExpenseCommand, Guid>
    {
        private readonly IExpenseRepository _expenses;
        private readonly IGroupRepository _groups;
        private readonly IAccountRepository _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ExpensePostingService _postingService;
        private readonly ICurrentUser _currentUser;

        public ReviseExpenseCommandHandler(
            IExpenseRepository expenses,
            IGroupRepository groups,
            IAccountRepository accounts,
            IUnitOfWork unitOfWork,
            ExpensePostingService postingService,
            ICurrentUser currentUser)
        {
            _expenses = expenses;
            _groups = groups;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _postingService = postingService;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(ReviseExpenseCommand request, CancellationToken cancellationToken)
        {
            var original = await _expenses.GetByIdAsync(request.ExpenseId, cancellationToken)
                ?? throw new ExpenseNotFoundException(request.ExpenseId);

            var group = await _groups.GetByIdAsync(original.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(original.GroupId);

            if (original.PayerId != _currentUser.UserId)
                throw new InvalidExpenseException("Only the payer can revise an expense.");

            var accounts = new Dictionary<Guid, Account>();

            var everyone = original.Splits.Select(s => s.ParticipantId)
                .Concat(request.Participants)
                .Distinct();

            foreach (var participantId in everyone)
            {
                accounts[participantId] = await ResolveAccountAsync(participantId, group.Currency, cancellationToken);
            }

            original.Reverse(_currentUser.UserId, DateTimeOffset.UtcNow);
            _postingService.Reverse(original, accounts);

            var revision = Expense.Create(
                Guid.NewGuid(),
                group,
                original.PayerId,
                _currentUser.UserId,
                request.Amount,
                request.Description,
                request.OccurredAt,
                request.Participants,
                request.FixedShares,
                original.Id);

            _postingService.Post(revision, accounts);
            await _expenses.AddAsync(revision, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return revision.Id;
        }

        private async Task<Account> ResolveAccountAsync(Guid ownerId, string currency, CancellationToken cancellationToken)
        {
            var existing = await _accounts.GetByOwnerAndCurrencyAsync(ownerId, currency, cancellationToken);
            if (existing is not null)
                return existing;

            var account = new Account(Guid.NewGuid(), ownerId, currency);
            await _accounts.AddAsync(account, cancellationToken);
            return account;
        }
    }
}
