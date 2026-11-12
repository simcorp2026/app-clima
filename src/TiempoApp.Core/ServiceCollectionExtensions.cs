using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TiempoApp.Core.Options;
using TiempoApp.Core.Services;

namespace TiempoApp.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the OpenWeatherMap client as <see cref="IWeatherService"/>. Shared by Web and Desktop.</summary>
    public static IServiceCollection AddWeatherCore(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OpenWeatherOptions>()
            .Bind(configuration.GetSection(OpenWeatherOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<OpenWeatherClient>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<OpenWeatherOptions>>().Value;
                http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            })
            // Retries transient failures (5xx, 408, 429, network) with exponential backoff + jitter.
            // 401/404 are not transient, so they are never retried.
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.Retry.MaxRetryAttempts = 2;
                resilience.Retry.Delay = TimeSpan.FromMilliseconds(500);
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
            });

        // IWeatherService = cache decorator over the typed client.
        services.AddMemoryCache();
        services.AddTransient<IWeatherService>(sp =>
            ActivatorUtilities.CreateInstance<CachedWeatherService>(sp, sp.GetRequiredService<OpenWeatherClient>()));

        return services;
    }

    /// <summary>Registers <see cref="IWeatherService"/> as a client of a deployed TiempoApp.Web (no API key needed).</summary>
    public static IServiceCollection AddWeatherApiClient(this IServiceCollection services, Uri apiBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(apiBaseUrl);

        services.AddHttpClient<IWeatherService, WeatherApiClient>(http =>
                http.BaseAddress = new Uri(apiBaseUrl.AbsoluteUri.TrimEnd('/') + "/"))
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.Retry.MaxRetryAttempts = 2;
                resilience.Retry.Delay = TimeSpan.FromMilliseconds(500);
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(13);   // the server already retries upstream
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
