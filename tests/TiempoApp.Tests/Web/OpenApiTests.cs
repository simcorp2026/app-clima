using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace TiempoApp.Tests.Web;

public sealed class OpenApiTests(TiempoAppFactory factory) : IClassFixture<TiempoAppFactory>
{
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Development_ExposesApiDocs(string url)
    {
        using var client = factory.WithWebHostBuilder(b => b.UseEnvironment("Development")).CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Development_OpenApiDocumentsWeatherEndpoint()
    {
        using var client = factory.WithWebHostBuilder(b => b.UseEnvironment("Development")).CreateClient();

        var json = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/weather", json, StringComparison.Ordinal);
        Assert.Contains("GetCurrentWeather", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Production_HidesApiDocs(string url)
    {
        using var client = factory.WithWebHostBuilder(b => b.UseEnvironment("Production")).CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
