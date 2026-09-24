using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Common;
using Wallet.Domain.Users;

namespace Wallet.Api.Authentication
{
    public sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IUserRepository _users;

        public SessionAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IRefreshTokenRepository refreshTokens,
            IUserRepository users)
            : base(options, logger, encoder)
        {
            _refreshTokens = refreshTokens;
            _users = users;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = Request.Cookies[SessionAuthentication.CookieName];

            if (string.IsNullOrEmpty(token))
                return AuthenticateResult.NoResult();

            if (!SecureToken.HasFormat(token, RefreshToken.TokenPrefix))
                return AuthenticateResult.Fail("The session is not valid for this request.");

            var session = await _refreshTokens.GetByTokenAsync(token, Context.RequestAborted);

            if (session is null || !session.CanAuthenticateRequest(DateTimeOffset.UtcNow))
                return AuthenticateResult.Fail("The session is not valid for this request.");

            var user = await _users.GetByIdAsync(session.UserId, Context.RequestAborted);

            if (user is null)
                return AuthenticateResult.Fail("The session is not valid for this request.");

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim(SessionAuthentication.FamilyClaim, session.FamilyId.ToString())
                },
                Scheme.Name);

            return AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
        }
    }
}
