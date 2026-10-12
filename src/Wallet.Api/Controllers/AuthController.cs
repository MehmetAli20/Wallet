using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Authentication;
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
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponse>> Login(LoginRequest loginRequest, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new LoginCommand(loginRequest.Email, loginRequest.Password, ClientIp, LoginPurpose.TokenPair),
                cancellationToken);

            return ToTokenResponse(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new RefreshSessionCommand(request.RefreshToken, LoginPurpose.TokenPair), cancellationToken);

            return ToTokenResponse(result);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("session")]
        public async Task<IActionResult> StartSession(LoginRequest loginRequest, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new LoginCommand(
                    loginRequest.Email,
                    loginRequest.Password,
                    ClientIp,
                    LoginPurpose.BrowserSession,
                    Request.Cookies[SessionAuthentication.CookieName]),
                cancellationToken);

            WriteSessionCookie(result.Session!);

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("session/refresh")]
        public async Task<IActionResult> RefreshSession(CancellationToken cancellationToken)
        {
            var token = Request.Cookies[SessionAuthentication.CookieName];

            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            var result = await _sender.Send(new RefreshSessionCommand(token), cancellationToken);

            WriteSessionCookie(result.Session!);

            return NoContent();
        }

        [AllowAnonymous]
        [HttpDelete("session")]
        public async Task<IActionResult> EndSession(CancellationToken cancellationToken)
        {
            var token = Request.Cookies[SessionAuthentication.CookieName];

            if (!string.IsNullOrEmpty(token))
                await _sender.Send(new LogoutCommand(token), cancellationToken);

            Response.Cookies.Delete(SessionAuthentication.CookieName, SessionAuthentication.CookieOptions(null));

            return NoContent();
        }

        [AllowAnonymous]
        [RateLimit(RateLimitingOptions.AnonymousRegister)]
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest registerRequest, CancellationToken cancellationToken)
        {
            var userId = await _sender.Send(
                new RegisterCommand(registerRequest.Email, registerRequest.Password, registerRequest.DisplayName),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new RegisterResponse(userId));
        }

        private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        private void WriteSessionCookie(IssuedSession session) =>
            Response.Cookies.Append(
                SessionAuthentication.CookieName,
                session.Token,
                SessionAuthentication.CookieOptions(session.ExpiresAt));

        private static TokenResponse ToTokenResponse(LoginResult result) =>
            new(result.AccessToken!.Token, result.AccessToken.ExpiresAt, result.Session!.Token, result.Session.ExpiresAt);
    }
}
