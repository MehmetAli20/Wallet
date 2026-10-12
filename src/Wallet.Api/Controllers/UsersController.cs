using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Authentication;
using Wallet.Api.Contracts.Users;
using Wallet.Application.Users.GetCurrentUser;
using Wallet.Application.Users.RenameCurrentUser;
using Wallet.Application.Users.RevokeAllSessions;

namespace Wallet.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ISender _sender;

        public UsersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("me")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<CurrentUserResponse>> GetMe(CancellationToken cancellationToken)
        {
            var user = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);

            return user.ToCurrentUserResponse();
        }

        [HttpPatch("me")]
        public async Task<ActionResult<CurrentUserResponse>> UpdateMe(
            UpdateCurrentUserRequest request, CancellationToken cancellationToken)
        {
            var user = await _sender.Send(new RenameCurrentUserCommand(request.DisplayName), cancellationToken);

            return user.ToCurrentUserResponse();
        }

        [HttpDelete("me/sessions")]
        public async Task<IActionResult> EndAllSessions(CancellationToken cancellationToken)
        {
            await _sender.Send(new RevokeAllSessionsCommand(), cancellationToken);

            Response.Cookies.Delete(SessionAuthentication.CookieName, SessionAuthentication.CookieOptions(null));

            return NoContent();
        }
    }
}
