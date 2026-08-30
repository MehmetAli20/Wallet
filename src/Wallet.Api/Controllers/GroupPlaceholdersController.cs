using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Common;
using Wallet.Api.Contracts.Placeholders;
using Wallet.Application.Groups.Placeholders;

namespace Wallet.Api.Controllers
{
    [Route("api/v1/groups")]
    [ApiController]
    public class GroupPlaceholdersController : ControllerBase
    {
        private readonly ISender _sender;

        public GroupPlaceholdersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("{groupId:guid}/placeholders")]
        public async Task<ActionResult<CreatedResponse>> AddPlaceholder(
            Guid groupId,
            AddPlaceholderRequest request,
            CancellationToken cancellationToken)
        {
            var id = await _sender.Send(
                new AddPlaceholderCommand(groupId, request.DisplayName), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new CreatedResponse(id));
        }
    }
}
