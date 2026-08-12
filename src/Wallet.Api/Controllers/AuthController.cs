using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Users;
using Wallet.Application.Users.Login;

namespace Wallet.Api.Controllers
{
    [Route("api/[controller]")]
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
            var token = await _sender.Send(new LoginCommand(loginRequest.Username, loginRequest.Password), cancellationToken);

            return new LoginResponse(token);
        }
    }
}