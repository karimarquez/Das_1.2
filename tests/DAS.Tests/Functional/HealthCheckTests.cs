using System.Net;
using System.Text.Json;

namespace DAS.Tests.Functional;

public sealed class HealthCheckTests(DasWebApplicationFactory factory) : IClassFixture<DasWebApplicationFactory>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ReturnOnlyPublicStatus(string path)
    {
        using var client = factory.CreateClientAs(role: null);
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Single(body.RootElement.EnumerateObject());
    }
}
