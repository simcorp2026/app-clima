namespace TiempoApp.Tests.Web;

public sealed class StaticFrontendTests(TiempoAppFactory factory) : IClassFixture<TiempoAppFactory>
{
    [Theory]
    [InlineData("/", "text/html")]
    [InlineData("/app.js", "text/javascript")]
    [InlineData("/styles.css", "text/css")]
    public async Task Frontend_AssetsAreServed(string url, string expectedMediaType)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType?.MediaType);
    }
}
