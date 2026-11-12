using System.Net;

namespace TiempoApp.Tests.Web;

public sealed class HealthEndpointTests(TiempoAppFactory factory) : IClassFixture<TiempoAppFactory>
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
