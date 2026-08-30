using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Common;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Application.Groups.EnsurePair;

namespace Wallet.Api.Controllers
{
    [Route("api/v1/groups")]
    [ApiController]
    public class PairsController : ControllerBase
    {
        private readonly ISender _sender;

        public PairsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("pairs")]
        public async Task<ActionResult<CreatedResponse>> EnsurePair(
            EnsurePairRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new EnsurePairCommand(request.UserId, request.Currency), cancellationToken);

            var body = new CreatedResponse(result.GroupId);

            return result.Created
                ? StatusCode(StatusCodes.Status201Created, body)
                : Ok(body);
        }
    }
}
