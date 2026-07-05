using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CareTrack.IntegrationTests;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "invalid@test.com",
            password = "WrongPass1"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
