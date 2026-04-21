using GMServerWebPanel.API.Controllers;
using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GMServerWebPanel.API.UnitTests
{
    internal class TestJwtService(
        string issuer = "ТестовыйДомен",
        string audience = "ТестовыйДомен",
        string key = "ТестовыйХакерскийКлюч32СимволаМинимум",
        int expiresMinutes = 30,
        int expiresDays = 7
        ) : ITokenServise
    {
        public string GenerateToken(User user, bool rememberMe)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Login)
            };

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

            var creds = new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256);

            var time = (rememberMe ? DateTime.UtcNow.AddDays(expiresDays) : DateTime.UtcNow.AddMinutes(expiresMinutes));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: time,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    internal class TestUserService : IUserService
    {
        internal required List<User> _users;

        public User? GetUserBy(string login, string password)
        {
            return _users.FirstOrDefault(u =>
                u.Login == login && u.Password == password);
        }
    }

    public class AuthControllerTests
    {
        [Fact]
        public void LoginWithCorrectUserData_ReturnsOkObjectResult_And_ValidateToken()
        {
            // Arrange
            var login = "TestLogin";
            var password = "TestPassword";

            var userService = new TestUserService()
            {
                _users = [
                    new User { Login = login, Password = password }
                ]
            };

            var controller = new AuthController(userService, new TestJwtService());

            // Act
            var response = controller.Login(new LoginRequest()
            {
                Login = login,
                Password = password
            });

            // Assert
            var result = Assert.IsType<OkObjectResult>(response);

            var value = result.Value;
            var tokenProperty = value?.GetType().GetProperty("token");

            Assert.NotNull(tokenProperty);

            var token = tokenProperty.GetValue(value)?.ToString();
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public void LoginInvalidUserData_ReturnsUnauthorizedResult()
        {
            // Arrange
            var login = "TestLogin";
            var password = "TestPassword";

            var invalidLogin = login + "Invalid";
            var invalidPassword = password + "Invalid";

            var userService = new TestUserService()
            {
                _users = [
                    new User { Login = login, Password = password }
                ]
            };

            var controller = new AuthController(userService, new TestJwtService());


            // Act
            var response = controller.Login(new LoginRequest()
            {
                Login = invalidLogin,
                Password = invalidPassword
            });

            // Assert
            Assert.IsType<UnauthorizedResult>(response);
        }

        [Fact]
        public void LoginWithCorrectUserData_TestTokenLifeTime()
        {
            // Arrange
            var login = "TestLogin";
            var password = "TestPassword";

            var userService = new TestUserService()
            {
                _users = [
                    new User { Login = login, Password = password }
                ]
            };

            var jwtService = new TestJwtService();
            var controller = new AuthController(userService, jwtService);

            // Act
            var responseShort = controller.Login(new LoginRequest
            {
                Login = login,
                Password = password,
                RememberMe = false
            });

            var responseLong = controller.Login(new LoginRequest
            {
                Login = login,
                Password = password,
                RememberMe = true
            });

            // Extract tokens
            static string GetToken(IActionResult response)
            {
                var result = (OkObjectResult)response;
                var value = result.Value!;
                return value.GetType().GetProperty("token")!.GetValue(value)!.ToString()!;
            }

            var tokenShort = GetToken(responseShort);
            var tokenLong = GetToken(responseLong);

            var handler = new JwtSecurityTokenHandler();

            var jwtShort = handler.ReadJwtToken(tokenShort);
            var jwtLong = handler.ReadJwtToken(tokenLong);

            // Assert
            Assert.True(jwtLong.ValidTo > jwtShort.ValidTo);
        }
    }
}
