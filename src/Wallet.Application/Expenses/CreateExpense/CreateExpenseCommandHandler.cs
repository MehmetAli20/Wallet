using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.CreateExpense
{
    public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Guid>
    {
        private readonly IExpenseRepository _expenses;
        private readonly IGroupRepository _groups;
        private readonly IAccountRepository _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ExpensePostingService _postingService;
        private readonly ICurrentUser _currentUser;

        public CreateExpenseCommandHandler(
            IExpenseRepository expenseRepository,
            IGroupRepository groups,
            IAccountRepository accounts,
            IUnitOfWork unitOfWork,
            ExpensePostingService postingService,
            ICurrentUser currentUser)
        {
            _expenses = expenseRepository;
            _groups = groups;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _postingService = postingService;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken) ?? throw new GroupNotFoundException(request.GroupId);

            var expense = Expense.Create(
                Guid.NewGuid(),
                group,
                request.PayerId,
                _currentUser.UserId,
                request.Amount,
                request.Description,
                request.OccurredAt,
                request.Participants,
                request.FixedShares
                );

            var accounts = new Dictionary<Guid, Account>();

            foreach(var participantId in request.Participants.Distinct())
            {
                accounts[participantId] = await ResolveAccountAsync(participantId, group.Currency, cancellationToken);
            }

            _postingService.Post(expense, accounts);
            await _expenses.AddAsync(expense, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return expense.Id;
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