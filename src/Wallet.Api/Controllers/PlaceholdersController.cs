using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Placeholders;
using Wallet.Application.Groups.Placeholders;

namespace Wallet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaceholdersController : ControllerBase
    {
        private readonly ISender _sender;

        public PlaceholdersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("{placeholderUserId:guid}/claim-token")]
        public async Task<ActionResult<ClaimTokenResponse>> IssueClaimToken(
            Guid placeholderUserId,
            CancellationToken cancellationToken)
        {
            var token = await _sender.Send(
                new IssueClaimTokenCommand(placeholderUserId), cancellationToken);

            return Ok(new ClaimTokenResponse(token));
        }

        [AllowAnonymous]
        [HttpPost("claim")]
        public async Task<ActionResult<Guid>> Claim(
            ClaimPlaceholderRequest request,
            CancellationToken cancellationToken)
        {
            var id = await _sender.Send(new ClaimPlaceholderCommand(
                request.Token, request.Username, request.Email, request.Password), cancellationToken);

            return Ok(id);
        }
    }
}
