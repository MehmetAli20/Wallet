using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Configuration;
using Wallet.Api.Contracts.Users;
using Wallet.Api.RateLimiting;
using Wallet.Application.Users.Login;
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
            var token = await _sender.Send(new LoginCommand(loginRequest.Username, loginRequest.Password), cancellationToken);

            return new LoginResponse(token);
        }

        [AllowAnonymous]
        [RateLimit(RateLimitingOptions.AnonymousRegister)]
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest registerRequest, CancellationToken cancellationToken)
        {
            var userId = await _sender.Send(new RegisterCommand(registerRequest.Username, registerRequest.Email, registerRequest.Password), cancellationToken);
            
            return StatusCode(StatusCodes.Status201Created, new RegisterResponse(userId));
        }
    }
}