using MediatR;
using Wallet.Domain.Users;

namespace Wallet.Application.Groups.GetGroupPlaceholders
{
    public record GetGroupPlaceholdersQuery(Guid GroupId) : IRequest<IReadOnlyList<User>>;
}
