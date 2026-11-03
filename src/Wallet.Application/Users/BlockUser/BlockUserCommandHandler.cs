using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.BlockUser
{
    public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand>
    {
        private readonly IUserRepository _users;
        private readonly IUserBlockRepository _blocks;
        private readonly IGroupRepository _groups;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public BlockUserCommandHandler(
            IUserRepository users,
            IUserBlockRepository blocks,
            IGroupRepository groups,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _users = users;
            _blocks = blocks;
            _groups = groups;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task Handle(BlockUserCommand request, CancellationToken cancellationToken)
        {
            var blockerId = _currentUser.UserId;

            var target = await _users.GetByIdAsync(request.UserId, cancellationToken);

            if (target is null || target.IsDeleted)
                throw new UserNotFoundException(request.UserId);

            if (await _blocks.GetAsync(blockerId, request.UserId, cancellationToken) is not null)
                return;

            await _blocks.AddAsync(new UserBlock(blockerId, request.UserId, DateTimeOffset.UtcNow), cancellationToken);

            foreach (var group in await _groups.GetWithInvitationsBetweenAsync(blockerId, request.UserId, cancellationToken))
                group.DropInvitationsBetween(blockerId, request.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
