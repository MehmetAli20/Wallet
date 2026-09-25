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
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new LoginCommand(loginRequest.Email, loginRequest.Password, ClientIp, LoginPurpose.AccessToken),
                cancellationToken);

            return new LoginResponse(result.AccessToken!);
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

            var session = await _sender.Send(new RefreshSessionCommand(token), cancellationToken);

            WriteSessionCookie(session);

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
    }
}
