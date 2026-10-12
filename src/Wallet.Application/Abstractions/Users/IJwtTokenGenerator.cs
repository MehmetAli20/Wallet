using Wallet.Application.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface IJwtTokenGenerator
    {
        IssuedAccessToken GenerateToken(User user, Guid sessionFamilyId);
    }
}
