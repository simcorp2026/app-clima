namespace TiempoApp.Core.Models;

/// <summary>Our own contract towards the UIs (web and desktop).</summary>
/// <param name="Units">"metric" (°C, m/s), "imperial" (°F, mph) or "standard" (K, m/s).</param>
/// <param name="ObservedAt">Data time expressed in the city's local UTC offset.</param>
public sealed record WeatherDto(
    string City,
    string Country,
    double Temperature,
    double FeelsLike,
    int Humidity,
    double WindSpeed,
    string Description,
    string Icon,
    string IconUrl,
    string Units,
    DateTimeOffset ObservedAt);
