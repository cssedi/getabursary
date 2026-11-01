using System.Net;
using System.Net.Http.Json;
using GetABursary.Api.DTOs.Health;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GetABursary.Api.Tests.Health;

public class HealthEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_Health_ReturnsOkWithStatusOk()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
    }
}
