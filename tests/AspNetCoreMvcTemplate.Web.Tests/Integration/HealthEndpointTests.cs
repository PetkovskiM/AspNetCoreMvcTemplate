using System.Net;

namespace AspNetCoreMvcTemplate.Web.Tests.Integration;

// Najprostiot moshen test koj go potvrduva deka:
//   - Factory-to mozhe da boot-ne aplikacijata
//   - SQLite zamenata na DbContext-ot raboti (health check baras DbContextCheck)
//   - HTTP pipeline-ot odgovara
public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }
}
