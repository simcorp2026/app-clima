using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TiempoApp.Core.Models;
using TiempoApp.Core.Services;

namespace TiempoApp.Tests.Web;

public sealed class WeatherEndpointTests(TiempoAppFactory factory) : IClassFixture<TiempoAppFactory>
{
    private sealed class StubWeatherService(Func<string, WeatherDto> behavior) : IWeatherService
    {
        public Task<WeatherDto> GetCurrentAsync(string city, CancellationToken cancellationToken = default) =>
            Task.FromResult(behavior(city));
    }

    private HttpClient CreateClient(Func<string, WeatherDto> behavior) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddTransient<IWeatherService>(_ => new StubWeatherService(behavior))))
            .CreateClient();

    [Fact]
    public async Task Get_ValidCity_ReturnsWeather()
    {
        using var client = CreateClient(city =>
            new WeatherDto(city, "EC", 17, 16, 60, 3, "nubes", "04d", "https://x/04d@2x.png", "metric", DateTimeOffset.UnixEpoch));

        var weather = await client.GetFromJsonAsync<WeatherDto>("/api/weather?city=Quito");

        Assert.Equal("Quito", weather?.City);
    }

    [Theory]
    [InlineData("/api/weather")]
    [InlineData("/api/weather?city=%20%20")]
    public async Task Get_MissingCity_Returns400ValidationProblem(string url)
    {
        using var client = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("city", problem!.Errors.Keys);
    }

    [Theory]
    [InlineData(WeatherErrorKind.CityNotFound, HttpStatusCode.NotFound)]
    [InlineData(WeatherErrorKind.RateLimited, HttpStatusCode.TooManyRequests)]
    [InlineData(WeatherErrorKind.Timeout, HttpStatusCode.GatewayTimeout)]
    [InlineData(WeatherErrorKind.InvalidApiKey, HttpStatusCode.ServiceUnavailable)]
    [InlineData(WeatherErrorKind.Unavailable, HttpStatusCode.BadGateway)]
    [InlineData(WeatherErrorKind.InvalidResponse, HttpStatusCode.BadGateway)]
    public async Task Get_ServiceError_ReturnsProblemDetails(WeatherErrorKind kind, HttpStatusCode expected)
    {
        using var client = CreateClient(_ => throw new WeatherServiceException(kind, "boom"));

        var response = await client.GetAsync("/api/weather?city=Quito");

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal((int)expected, problem!.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
    }
}
