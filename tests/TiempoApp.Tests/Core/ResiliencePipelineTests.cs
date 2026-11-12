using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiempoApp.Core;
using TiempoApp.Core.Services;
using TiempoApp.Tests.Fakes;

namespace TiempoApp.Tests.Core;

/// <summary>Exercises the real DI registration (AddWeatherCore) with a fake primary handler.</summary>
public sealed class ResiliencePipelineTests
{
    private static IWeatherService BuildService(FakeHttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OpenWeather:ApiKey"] = "test-key" })
            .Build();

        var services = new ServiceCollection().AddLogging().AddWeatherCore(configuration);
        services.AddHttpClient<OpenWeatherClient>().ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider().GetRequiredService<IWeatherService>();
    }

    [Fact]
    public async Task TransientFailure_IsRetried()
    {
        var handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable, ""),
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, SampleResponses.QuitoOk));

        var weather = await BuildService(handler).GetCurrentAsync("Quito");

        Assert.Equal("Quito", weather.City);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task NotFound_IsNotRetried()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.NotFound, SampleResponses.NotFound);

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(() => BuildService(handler).GetCurrentAsync("Nowhere"));

        Assert.Equal(WeatherErrorKind.CityNotFound, ex.Kind);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SuccessfulLookup_IsServedFromCacheAfterwards()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, SampleResponses.QuitoOk);
        var service = BuildService(handler);

        await service.GetCurrentAsync("Quito");
        await service.GetCurrentAsync("quito");

        Assert.Single(handler.Requests);
    }
}
