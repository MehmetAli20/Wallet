using MediatR;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Groups.GetGroupPlaceholders
{
    public class GetGroupPlaceholdersQueryHandler
        : IRequestHandler<GetGroupPlaceholdersQuery, IReadOnlyList<User>>
    {
        private readonly IUserRepository _users;

        public GetGroupPlaceholdersQueryHandler(IUserRepository users)
        {
            _users = users;
        }

        public async Task<IReadOnlyList<User>> Handle(
            GetGroupPlaceholdersQuery request, CancellationToken cancellationToken) =>
            await _users.GetPlaceholdersInGroupAsync(request.GroupId, cancellationToken);
    }
}
