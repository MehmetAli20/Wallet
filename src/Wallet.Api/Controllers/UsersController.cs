using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Users;
using Wallet.Application.Users.GetCurrentUser;
using Wallet.Application.Users.RenameCurrentUser;

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
    }
}
