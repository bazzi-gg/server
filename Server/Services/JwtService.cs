using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Server.Models.Etc;
using Server.Models.Request;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Server.Services
{
    public interface IJwtService
    {
        string Authenticate(AuthenticateRequest model);
    }

    public class JwtService : IJwtService
    {
        readonly JwtOptions _jwtOptions;
        public JwtService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }
        private static readonly TimeSpan Expires = TimeSpan.FromDays(1);
        private static DateTime CreateExpiryDate()
        {
            return DateTime.UtcNow.Add(Expires);
        }
        public string Authenticate(AuthenticateRequest model)
        {
            PasswordHasher<AuthenticateRequest> passwordHasher = new();
            string hashed = passwordHasher.HashPassword(model, model.Key);
            if (passwordHasher.VerifyHashedPassword(model, hashed, _jwtOptions.Key) != PasswordVerificationResult.Success)
            {
                return null;
            }
            DateTime expiryDate = CreateExpiryDate();
            // authentication successful so generate jwt and refresh tokens
            var jwtToken = GenerateJwtToken("bazzi.gg", expiryDate);
            return jwtToken;
        }

        /// <summary>
        /// 토큰 생성
        /// </summary>
        /// <param name="str">식별자</param>
        /// <param name="expires">유효 기간(UTC 기준)</param>
        /// <returns></returns>
        private string GenerateJwtToken(string str, DateTime expires)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(_jwtOptions.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, str)
                }),
                Expires = expires,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha384)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }

}
