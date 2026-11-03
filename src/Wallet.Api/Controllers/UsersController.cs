using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Authentication;
using Wallet.Api.Contracts.Users;
using Wallet.Application.Users.BlockUser;
using Wallet.Application.Users.DeleteCurrentUser;
using Wallet.Application.Users.GetBlockedUsers;
using Wallet.Application.Users.GetCurrentUser;
using Wallet.Application.Users.RenameCurrentUser;
using Wallet.Application.Users.RevokeAllSessions;
using Wallet.Application.Users.UnblockUser;

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

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMe(
            [FromBody] DeleteCurrentUserRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteCurrentUserCommand(request.Password), cancellationToken);

            Response.Cookies.Delete(SessionAuthentication.CookieName, SessionAuthentication.CookieOptions(null));

            return NoContent();
        }

        [HttpGet("me/blocks")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<List<BlockedUserResponse>>> GetBlocks(CancellationToken cancellationToken)
        {
            var blocked = await _sender.Send(new GetBlockedUsersQuery(), cancellationToken);

            return blocked.Select(b => b.ToBlockedUserResponse()).ToList();
        }

        [HttpPut("me/blocks/{userId:guid}")]
        public async Task<IActionResult> Block(Guid userId, CancellationToken cancellationToken)
        {
            await _sender.Send(new BlockUserCommand(userId), cancellationToken);

            return NoContent();
        }

        [HttpDelete("me/blocks/{userId:guid}")]
        public async Task<IActionResult> Unblock(Guid userId, CancellationToken cancellationToken)
        {
            await _sender.Send(new UnblockUserCommand(userId), cancellationToken);

            return NoContent();
        }
    }
}
