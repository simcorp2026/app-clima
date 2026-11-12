using System.ComponentModel.DataAnnotations;

namespace TiempoApp.Core.Options;

/// <summary>Settings bound from the "OpenWeather" configuration section.</summary>
public sealed class OpenWeatherOptions
{
    public const string SectionName = "OpenWeather";

    /// <summary>Secret: User Secrets locally, env var <c>OpenWeather__ApiKey</c> in the cloud.</summary>
    [Required(ErrorMessage = "OpenWeather:ApiKey is not configured (use User Secrets or the OpenWeather__ApiKey env var).")]
    public string ApiKey { get; set; } = string.Empty;

    [Required, Url]
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/2.5/";

    [Required, Url]
    public string IconBaseUrl { get; set; } = "https://openweathermap.org/img/wn/";

    [AllowedValues("standard", "metric", "imperial")]
    public string Units { get; set; } = "metric";

    [Required]
    public string Lang { get; set; } = "es";

    [Range(0, 1440)]
    public int CacheMinutes { get; set; } = 10;
}
