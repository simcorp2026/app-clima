using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TiempoApp.Core.Models;
using TiempoApp.Core.Options;
using TiempoApp.Core.Services;

namespace TiempoApp.Tests.Core;

public sealed class CachedWeatherServiceTests
{
    private sealed class CountingWeatherService(Func<string, WeatherDto>? factory = null) : IWeatherService
    {
        public int Calls { get; private set; }

        public Task<WeatherDto> GetCurrentAsync(string city, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult((factory ?? Sample)(city));
        }
    }

    private static WeatherDto Sample(string city) =>
        new(city, "EC", 17, 16, 60, 3, "nubes", "04d", "https://x/04d@2x.png", "metric", DateTimeOffset.UnixEpoch);

    private static CachedWeatherService CreateSut(IWeatherService inner, int cacheMinutes = 10) =>
        new(inner,
            new MemoryCache(new MemoryCacheOptions()),
            Microsoft.Extensions.Options.Options.Create(new OpenWeatherOptions { ApiKey = "k", CacheMinutes = cacheMinutes }),
            NullLogger<CachedWeatherService>.Instance);

    [Fact]
    public async Task GetCurrentAsync_SameCityDifferentCasingAndSpaces_HitsProviderOnce()
    {
        var inner = new CountingWeatherService();
        var sut = CreateSut(inner);

        var first = await sut.GetCurrentAsync("Quito");
        var second = await sut.GetCurrentAsync("  QUITO ");

        Assert.Equal(1, inner.Calls);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetCurrentAsync_DifferentCities_AreCachedSeparately()
    {
        var inner = new CountingWeatherService();
        var sut = CreateSut(inner);

        await sut.GetCurrentAsync("Quito");
        await sut.GetCurrentAsync("Lima");

        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task GetCurrentAsync_Errors_AreNotCached()
    {
        var inner = new CountingWeatherService(_ =>
            throw new WeatherServiceException(WeatherErrorKind.Unavailable, "down"));
        var sut = CreateSut(inner);

        await Assert.ThrowsAsync<WeatherServiceException>(() => sut.GetCurrentAsync("Quito"));
        await Assert.ThrowsAsync<WeatherServiceException>(() => sut.GetCurrentAsync("Quito"));

        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task GetCurrentAsync_CacheDisabled_AlwaysCallsProvider()
    {
        var inner = new CountingWeatherService();
        var sut = CreateSut(inner, cacheMinutes: 0);

        await sut.GetCurrentAsync("Quito");
        await sut.GetCurrentAsync("Quito");

        Assert.Equal(2, inner.Calls);
    }
}
