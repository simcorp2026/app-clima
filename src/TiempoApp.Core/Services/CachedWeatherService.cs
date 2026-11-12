using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TiempoApp.Core.Models;
using TiempoApp.Core.Options;

namespace TiempoApp.Core.Services;

/// <summary>Decorator that caches successful lookups per city (errors are never cached).</summary>
public sealed partial class CachedWeatherService(
    IWeatherService inner,
    IMemoryCache cache,
    IOptions<OpenWeatherOptions> options,
    ILogger<CachedWeatherService> logger) : IWeatherService
{
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(options.Value.CacheMinutes);

    public async Task<WeatherDto> GetCurrentAsync(string city, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        if (_ttl <= TimeSpan.Zero)
        {
            return await inner.GetCurrentAsync(city, cancellationToken);
        }

        var key = CacheKey(city);
        if (cache.TryGetValue(key, out WeatherDto? cached) && cached is not null)
        {
            LogCacheHit(logger, key);
            return cached;
        }

        var weather = await inner.GetCurrentAsync(city, cancellationToken);
        cache.Set(key, weather, _ttl);
        LogFetched(logger, weather.City, weather.Country, weather.Temperature, _ttl.TotalMinutes);
        return weather;
    }

    // "  QUITO " and "quito" share the same entry.
    private static string CacheKey(string city) => $"weather:{city.Trim().ToLowerInvariant()}";

    [LoggerMessage(Level = LogLevel.Information, Message = "Weather cache hit for {CacheKey}")]
    private static partial void LogCacheHit(ILogger logger, string cacheKey);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Weather fetched from provider: {City},{Country} {Temperature} (cached {CacheMinutes} min)")]
    private static partial void LogFetched(ILogger logger, string city, string country, double temperature, double cacheMinutes);
}
