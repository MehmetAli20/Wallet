using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Authentication
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtOptions _options; 

        public JwtTokenGenerator(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
                Claims = new Dictionary<string, object>
                {
                    [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                    [JwtRegisteredClaimNames.UniqueName] = user.Username,
                    [ClaimTypes.Role] = user.Role.ToString()
                }
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}
