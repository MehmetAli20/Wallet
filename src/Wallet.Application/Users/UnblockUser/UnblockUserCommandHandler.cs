using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.UnblockUser
{
    public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand>
    {
        private readonly IUserBlockRepository _blocks;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public UnblockUserCommandHandler(
            IUserBlockRepository blocks, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _blocks = blocks;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(UnblockUserCommand request, CancellationToken cancellationToken)
        {
            var block = await _blocks.GetAsync(_currentUser.UserId, request.UserId, cancellationToken);

            if (block is null)
                return;

            _blocks.Remove(block);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
