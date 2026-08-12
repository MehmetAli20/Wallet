using System.Security.Claims;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Api.Authentication
{
    public class HttpContextCurrentUser :ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpContextCurrentUser(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public bool IsSystem => false;

        public Guid UserId
        {
            get
            {
                var principal = _accessor.HttpContext?.User
                    ?? throw new InvalidOperationException("No HTTP context in the current scope.");

                var values = principal.FindAll(ClaimTypes.NameIdentifier)
                    .Select(c => c.Value)
                    .Distinct()
                    .ToArray();

                if(values.Length != 1 
                    || !Guid.TryParse(values[0], out var userId) 
                    || userId == Guid.Empty)
                {
                    throw new InvalidOperationException("Could not resolve a single user id from the current principal.");
                }
                return userId;
            }
        }

    }
}