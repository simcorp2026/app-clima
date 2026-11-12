using System.Net;
using TiempoApp.Core.Services;
using TiempoApp.Tests.Fakes;

namespace TiempoApp.Tests.Core;

public sealed class WeatherApiClientTests
{
    private const string WeatherJson = """
        {"city":"Quito","country":"EC","temperature":20.5,"feelsLike":20.4,"humidity":66,"windSpeed":3.6,
         "description":"muy nuboso","icon":"04d","iconUrl":"https://openweathermap.org/img/wn/04d@2x.png",
         "units":"metric","observedAt":"2026-11-12T11:10:08-05:00"}
        """;

    private static WeatherApiClient CreateClient(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://tiempo.example/") });

    [Fact]
    public async Task GetCurrentAsync_Success_DeserializesOwnContract()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, WeatherJson);

        var weather = await CreateClient(handler).GetCurrentAsync(" Quito ");

        Assert.Equal("Quito", weather.City);
        Assert.Equal(TimeSpan.FromHours(-5), weather.ObservedAt.Offset);
        Assert.Equal("https://tiempo.example/api/weather?city=Quito", Assert.Single(handler.Requests).RequestUri!.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, WeatherErrorKind.CityNotFound)]
    [InlineData(HttpStatusCode.TooManyRequests, WeatherErrorKind.RateLimited)]
    [InlineData(HttpStatusCode.GatewayTimeout, WeatherErrorKind.Timeout)]
    [InlineData(HttpStatusCode.ServiceUnavailable, WeatherErrorKind.InvalidApiKey)]
    [InlineData(HttpStatusCode.BadGateway, WeatherErrorKind.Unavailable)]
    public async Task GetCurrentAsync_ProblemStatus_MapsBackToKind(HttpStatusCode status, WeatherErrorKind expected)
    {
        var handler = FakeHttpMessageHandler.Returning(status, """{"title":"x","status":0}""");

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(() => CreateClient(handler).GetCurrentAsync("Quito"));

        Assert.Equal(expected, ex.Kind);
    }

    [Fact]
    public async Task GetCurrentAsync_ServerDown_ThrowsUnavailable()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("refused"));

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(() => CreateClient(handler).GetCurrentAsync("Quito"));

        Assert.Equal(WeatherErrorKind.Unavailable, ex.Kind);
    }
}
