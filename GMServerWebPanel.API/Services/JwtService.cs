using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GMServerWebPanel.API.Services
{

    public class JwtService(IOptions<JwtSettings> settings) : ITokenServise
    {
        private readonly JwtSettings _settings = settings.Value;

        public string GenerateToken(User user, bool rememberMe)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Login)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key));

            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var time = (rememberMe ? DateTime.UtcNow.AddDays(_settings.ExpiresDays) : DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes));

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: time,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
