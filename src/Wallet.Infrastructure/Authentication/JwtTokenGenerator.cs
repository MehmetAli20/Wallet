using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Users;
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

        public IssuedAccessToken GenerateToken(User user, Guid sessionFamilyId)
        {
            if (user.IsPlaceholder)
                throw new InvalidOperationException("A placeholder user cannot receive an access token.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
                Claims = new Dictionary<string, object>
                {
                    [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                    [ClaimTypes.Role] = user.Role.ToString(),
                    [SessionClaims.Family] = sessionFamilyId.ToString()
                }
            };

            return new IssuedAccessToken(
                new JsonWebTokenHandler().CreateToken(descriptor), new DateTimeOffset(expiresAt, TimeSpan.Zero));
        }
    }
}
