using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Services;
using GMServerWebPanel.API.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace GMServerWebPanel.API.UnitTests
{
    public class JwtServiceTests
    {
        [Fact]
        public void GenerateToken_ReturnsNotEmptyString()
        {
            // Arrange
            var jwtSettings = Options.Create(new JwtSettings()
            {
                Key = "ХакерскийКлючСоздайтеСвой32СимволаИспользуйтеГенераторПаролей",
                Issuer = "TestDomain",
                Audience = "TestDomain",
                ExpiresMinutes = 30,
                ExpiresDays = 7
            });

            var jwtService = new JwtService(jwtSettings);

            var user = new User()
            {
                Login = "TestLogin",
                Password = "TestPassword"
            };

            // Act
            var tokenString = jwtService.GenerateToken(user, false);

            // Assert
            Assert.False(string.IsNullOrEmpty(tokenString));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GenerateToken_TokenContainsCorrectClaims_AndFreshExpiration(bool rememberMe)
        {
            // Arrange
            var handler = new JwtSecurityTokenHandler();

            var jwtSettings = Options.Create(new JwtSettings()
            {
                Key = "ХакерскийКлючСоздайтеСвой32СимволаИспользуйтеГенераторПаролей",
                Issuer = "TestDomain",
                Audience = "TestDomain",
                ExpiresMinutes = 30,
                ExpiresDays = 7
            });

            var jwtService = new JwtService(jwtSettings);

            var user = new User()
            {
                Login = "TestLogin",
                Password = "TestPassword"
            };

            // Act
            var tokenString = jwtService.GenerateToken(user, rememberMe);

            var token = handler.ReadJwtToken(tokenString);

            // Assert
            var loginClaim = token.Claims.FirstOrDefault(c => c.Type == "Login");
            Assert.NotNull(loginClaim);
            Assert.Equal(user.Login, loginClaim.Value);

            Assert.Equal(jwtSettings.Value.Issuer, token.Issuer);
            Assert.Equal(jwtSettings.Value.Audience, token.Audiences.First());

            if (rememberMe)
                Assert.True((token.ValidTo - token.ValidFrom).TotalDays == jwtSettings.Value.ExpiresDays);
            else
                Assert.True((token.ValidTo - token.ValidFrom).TotalMinutes == jwtSettings.Value.ExpiresMinutes);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ValidateToken_GenerateFreshToken_ReturnsNotExpiredToken(bool rememberMe)
        {
            // Arrange
            var handler = new JwtSecurityTokenHandler();

            var jwtSettings = Options.Create(new JwtSettings()
            {
                Key = "ХакерскийКлючСоздайтеСвой32СимволаИспользуйтеГенераторПаролей",
                Issuer = "TestDomain",
                Audience = "TestDomain",
                ExpiresMinutes = 30,
                ExpiresDays = 7
            });

            var jwtService = new JwtService(jwtSettings);

            var user = new User()
            {
                Login = "TestLogin",
                Password = "TestPassword"
            };

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwtSettings.Value.Issuer,
                ValidAudience = jwtSettings.Value.Audience,
                IssuerSigningKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(jwtSettings.Value.Key)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            // Act
            var tokenString = jwtService.GenerateToken(user, rememberMe);

            handler.ValidateToken(tokenString, validationParameters, out var validatedToken);

            // Assert
            Assert.NotNull(validatedToken);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ValidateToken_GenerateExpiredToken_Throw_SecurityTokenExpiredException(bool rememberMe)
        {
            // Arrange
            var handler = new JwtSecurityTokenHandler();

            var expiredValue = -1;

            var jwtSettings = Options.Create(new JwtSettings()
            {
                Key = "ХакерскийКлючСоздайтеСвой32СимволаИспользуйтеГенераторПаролей",
                Issuer = "TestDomain",
                Audience = "TestDomain",
                NotBefore = DateTime.UtcNow.AddYears(expiredValue),
                ExpiresMinutes = expiredValue,
                ExpiresDays = expiredValue
            });

            var jwtService = new JwtService(jwtSettings);

            var user = new User()
            {
                Login = "TestLogin",
                Password = "TestPassword"
            };

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwtSettings.Value.Issuer,
                ValidAudience = jwtSettings.Value.Audience,
                IssuerSigningKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(jwtSettings.Value.Key)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };

            // Act
            var tokenString = jwtService.GenerateToken(user, rememberMe);

            // Assert
            Assert.Throws<SecurityTokenExpiredException>(() =>
                handler.ValidateToken(tokenString, validationParameters, out _));
        }

        [Fact]
        public void GenerateToken_CheckFakeSecretKey_Throw_SecurityTokenSignatureKeyNotFoundException()
        {
            // Arrange
            var handler = new JwtSecurityTokenHandler();

            var jwtSettings = Options.Create(new JwtSettings()
            {
                Key = "ХакерскийКлючСоздайтеСвой32СимволаИспользуйтеГенераторПаролей",
                Issuer = "TestDomain",
                Audience = "TestDomain",
                ExpiresMinutes = 30,
                ExpiresDays = 7
            });

            // Create fake key.
            var jwtFakeSecretKey = jwtSettings.Value.Key + "FakeKey!";

            var jwtService = new JwtService(jwtSettings);

            var user = new User()
            {
                Login = "TestLogin",
                Password = "TestPassword"
            };

            var wrongValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwtSettings.Value.Issuer,
                ValidAudience = jwtSettings.Value.Audience,
                IssuerSigningKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(jwtFakeSecretKey)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            // Act
            var tokenString = jwtService.GenerateToken(user, false);

            // Assert
            Assert.Throws<SecurityTokenSignatureKeyNotFoundException> (() =>
                handler.ValidateToken(tokenString, wrongValidationParameters, out _));
        }

        [Fact]
        public void ValidateToken_CheckSecurityAlgorithm_Expected_HmacSha256()
        {
            // Arrange
            var handler = new JwtSecurityTokenHandler();

            var jwtSettings = Options.Create(new JwtSettings()
            {
                Key = "ХакерскийКлючСоздайтеСвой32СимволаИспользуйтеГенераторПаролей",
                Issuer = "TestDomain",
                Audience = "TestDomain",
                ExpiresMinutes = 30,
                ExpiresDays = 7
            });

            var jwtService = new JwtService(jwtSettings);

            var user = new User()
            {
                Login = "TestLogin",
                Password = "TestPassword"
            };

            // Act
            var tokenString = jwtService.GenerateToken(user, false);

            var token = handler.ReadJwtToken(tokenString);

            // Assert
            Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        }
    }
}
