using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TiempoApp.Core.Options;
using TiempoApp.Core.Services;
using TiempoApp.Tests.Fakes;

namespace TiempoApp.Tests.Core;

public sealed class OpenWeatherClientTests
{
    private static OpenWeatherClient CreateClient(FakeHttpMessageHandler handler)
    {
        var options = new OpenWeatherOptions { ApiKey = "test-key" };
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.BaseUrl) };
        return new OpenWeatherClient(http, Microsoft.Extensions.Options.Options.Create(options), NullLogger<OpenWeatherClient>.Instance);
    }

    [Fact]
    public async Task GetCurrentAsync_Success_MapsToWeatherDto()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SampleResponses.QuitoOk);

        var weather = await CreateClient(handler).GetCurrentAsync("Quito");

        Assert.Equal("Quito", weather.City);
        Assert.Equal("EC", weather.Country);
        Assert.Equal(17.5, weather.Temperature);
        Assert.Equal(16.9, weather.FeelsLike);
        Assert.Equal(63, weather.Humidity);
        Assert.Equal(3.6, weather.WindSpeed);
        Assert.Equal("nubes rotas", weather.Description);
        Assert.Equal("04d", weather.Icon);
        Assert.Equal("https://openweathermap.org/img/wn/04d@2x.png", weather.IconUrl);
        Assert.Equal("metric", weather.Units);
        Assert.Equal(TimeSpan.FromHours(-5), weather.ObservedAt.Offset);
        Assert.Equal(1762963200, weather.ObservedAt.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetCurrentAsync_BuildsExpectedRequest()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SampleResponses.QuitoOk);

        await CreateClient(handler).GetCurrentAsync("  São Paulo ");

        var uri = Assert.Single(handler.Requests).RequestUri!;
        Assert.Equal("/data/2.5/weather", uri.AbsolutePath);
        Assert.Contains("q=S%C3%A3o%20Paulo", uri.Query, StringComparison.Ordinal);
        Assert.Contains("appid=test-key", uri.Query, StringComparison.Ordinal);
        Assert.Contains("units=metric", uri.Query, StringComparison.Ordinal);
        Assert.Contains("lang=es", uri.Query, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, SampleResponses.NotFound, WeatherErrorKind.CityNotFound)]
    [InlineData(HttpStatusCode.Unauthorized, SampleResponses.Unauthorized, WeatherErrorKind.InvalidApiKey)]
    [InlineData(HttpStatusCode.TooManyRequests, "", WeatherErrorKind.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, "<html>oops</html>", WeatherErrorKind.Unavailable)]
    public async Task GetCurrentAsync_HttpError_ThrowsMappedKind(HttpStatusCode status, string body, WeatherErrorKind expected)
    {
        var handler = FakeHttpMessageHandler.Returning(status, body);

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(() => CreateClient(handler).GetCurrentAsync("Quito"));

        Assert.Equal(expected, ex.Kind);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "name": "Quito", "weather": [] }""")]
    public async Task GetCurrentAsync_InvalidOrIncompleteJson_ThrowsInvalidResponse(string body)
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, body);

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(() => CreateClient(handler).GetCurrentAsync("Quito"));

        Assert.Equal(WeatherErrorKind.InvalidResponse, ex.Kind);
    }

    [Fact]
    public async Task GetCurrentAsync_NetworkError_ThrowsUnavailable()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(() => CreateClient(handler).GetCurrentAsync("Quito"));

        Assert.Equal(WeatherErrorKind.Unavailable, ex.Kind);
    }
}
