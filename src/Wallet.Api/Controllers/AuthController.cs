using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Configuration;
using Wallet.Api.Contracts.Users;
using Wallet.Api.RateLimiting;
using Wallet.Application.Users;
using Wallet.Application.Users.Login;
using Wallet.Application.Users.Logout;
using Wallet.Application.Users.RefreshSession;
using Wallet.Application.Users.Register;

namespace Wallet.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const string RefreshCookieName = "wallet_refresh";
        private const string RefreshCookiePath = "/api/v1/auth";

        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest, CancellationToken cancellationToken)
        {
            var tokens = await _sender.Send(
                new LoginCommand(
                    loginRequest.Username,
                    loginRequest.Password,
                    HttpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);

            SetRefreshCookie(tokens);

            return new LoginResponse(tokens.AccessToken);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshCookieName];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized();

            var tokens = await _sender.Send(new RefreshSessionCommand(refreshToken), cancellationToken);

            SetRefreshCookie(tokens);

            return new LoginResponse(tokens.AccessToken);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshCookieName];

            if (!string.IsNullOrEmpty(refreshToken))
                await _sender.Send(new LogoutCommand(refreshToken), cancellationToken);

            Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(expires: null));

            return NoContent();
        }

        [AllowAnonymous]
        [RateLimit(RateLimitingOptions.AnonymousRegister)]
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest registerRequest, CancellationToken cancellationToken)
        {
            var userId = await _sender.Send(new RegisterCommand(registerRequest.Username, registerRequest.Email, registerRequest.Password), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new RegisterResponse(userId));
        }

        private void SetRefreshCookie(AuthTokens tokens) =>
            Response.Cookies.Append(
                RefreshCookieName,
                tokens.RefreshToken,
                RefreshCookieOptions(tokens.RefreshTokenExpiresAt));

        private static CookieOptions RefreshCookieOptions(DateTimeOffset? expires) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = expires
        };
    }
}