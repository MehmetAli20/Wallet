using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Transfers;

namespace Wallet.Application.Transfers.TransferMoney
{
    public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand>
    {
        private readonly IAccountRepository _accounts;
        private readonly IGroupRepository _groups;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TransferService _transferService;
        private readonly ICurrentUser _currentUser;

        public TransferMoneyCommandHandler(
            IAccountRepository accounts,
            IGroupRepository groups,
            IUnitOfWork unitOfWork,
            TransferService transferService,
            ICurrentUser currentUser)
        {
            _accounts = accounts;
            _groups = groups;
            _unitOfWork = unitOfWork;
            _transferService = transferService;
            _currentUser = currentUser;
        }

        public async Task Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
        {
            if (request.RecipientUserId == _currentUser.UserId)
                throw new InvalidTransferException("Cannot transfer to yourself.");

            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            if (!group.IsActiveMember(request.RecipientUserId))
                throw new InvalidTransferException("The recipient is not an active member of this group.");

            var payer = await ResolveAccountAsync(_currentUser.UserId, group.Currency, cancellationToken);
            var payee = await ResolveAccountAsync(request.RecipientUserId, group.Currency, cancellationToken);

            _transferService.Settle(payer, payee, new Money(request.Amount, group.Currency), group.Id);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
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
