using GMServerWebPanel.API.Data;
using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GMServerWebPanel.API.IntegrationTests;

public class LoginPostTests(DBWebApplicationFactory factory) : IClassFixture<DBWebApplicationFactory>
{
    private readonly DBWebApplicationFactory _factory = factory;

    [Fact]
    public async Task LoginWithEmptyJson_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IncorrectUserData_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var content = new StringContent("""{"Login":"NoName","Password":"123"}""", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RightUserData_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            db.Database.EnsureCreated();

            db.Users.Add(new User
            {
                Login = "TestLogin2",
                Password = hasher.HashPassword("TestPassword2")
            });

            db.SaveChanges();
        }

        // Arrange
        //var client = _factory.CreateClient();

        // ОТПРАВЛЯЕМ СЫРОЙ ПАРОЛЬ, А НЕ ХЭШ
        var loginData = new { Login = "TestLogin2", Password = "TestPassword2" };
        var json = JsonSerializer.Serialize(loginData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Test2()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();
            if (!db.Users.Any(u => u.Login == "TestLogin"))
            {
                db.Users.Add(new User { Login = "TestLogin", Password = "TestPassword" });
                db.SaveChanges();
            }

            var user = db.Users.FirstOrDefault(u => u.Login == "TestLogin");

            Assert.NotNull(user);
        }
    }
}
