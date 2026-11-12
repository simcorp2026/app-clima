using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiempoApp.Core;

namespace TiempoApp.Desktop;

public enum WeatherSourceMode
{
    /// <summary>OpenWeatherMap directly, key from User Secrets. Local demo only.</summary>
    Direct,

    /// <summary>Through TiempoApp.Web: the API key stays on the server. Use this for any .exe given to third parties.</summary>
    Api,
}

public sealed class WeatherSourceOptions
{
    public const string SectionName = "WeatherSource";

    public WeatherSourceMode Mode { get; set; } = WeatherSourceMode.Direct;

    public Uri? ApiBaseUrl { get; set; }
}

internal static class WeatherSourceRegistration
{
    /// <summary>Registers IWeatherService according to "WeatherSource:Mode".</summary>
    public static IServiceCollection AddWeatherSource(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(WeatherSourceOptions.SectionName);
        var source = section.Get<WeatherSourceOptions>() ?? new WeatherSourceOptions();
        services.Configure<WeatherSourceOptions>(section);

        return source.Mode switch
        {
            WeatherSourceMode.Direct => services.AddWeatherCore(configuration),
            WeatherSourceMode.Api => services.AddWeatherApiClient(source.ApiBaseUrl
                ?? throw new InvalidOperationException("WeatherSource:ApiBaseUrl es obligatorio cuando Mode es 'Api'.")),
            _ => throw new InvalidOperationException($"WeatherSource:Mode '{source.Mode}' no es válido (usa 'Direct' o 'Api')."),
        };
    }
}
