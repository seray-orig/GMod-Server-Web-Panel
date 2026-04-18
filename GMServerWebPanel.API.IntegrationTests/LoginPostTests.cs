using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;

namespace GMServerWebPanel.API.IntegrationTests;

public class LoginPostTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

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
        // Arrange
        var client = _factory.CreateClient();
        var content = new StringContent("""{"Login":"TestUser","Password":"TestUser"}""", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
