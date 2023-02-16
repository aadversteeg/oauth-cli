using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Core.Infrastructure.ConsoleApp
{
    public class JwtCreator
    {
        public static string CreateTokenWithX509SigningCredentials(X509Certificate2 signingCert, string clientId, string tenantId)
        {
            return CreateTokenWithX509SigningCredentials(
                signingCert, 
                clientId, 
                clientId, 
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token");
        }

        public static string CreateTokenWithX509SigningCredentials(X509Certificate2 signingCert, string subject, string issuer, string audience)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim(JwtRegisteredClaimNames.Jti, System.Guid.NewGuid().ToString()),
            };

            var issuedAt = System.DateTime.UtcNow;
            var notBefore = issuedAt.AddMilliseconds(-30);
            var expires = issuedAt.AddSeconds(15);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                NotBefore = notBefore,
                IssuedAt = issuedAt,
                Audience = audience,
                Issuer = issuer,
                SigningCredentials = new X509SigningCredentials(signingCert)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            string tokenString = tokenHandler.WriteToken(token);

            return tokenString;
        }
    }
}
