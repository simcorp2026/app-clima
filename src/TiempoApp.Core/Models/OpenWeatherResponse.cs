namespace TiempoApp.Core.Models;

// Raw payload of GET /data/2.5/weather (only the fields we use).
// Deserialized with snake_case naming, so FeelsLike <- "feels_like".
public sealed record OpenWeatherResponse(
    string? Name,
    long Dt,
    int Timezone,
    OpenWeatherMain? Main,
    OpenWeatherWind? Wind,
    OpenWeatherSys? Sys,
    IReadOnlyList<OpenWeatherCondition>? Weather);

public sealed record OpenWeatherMain(double Temp, double FeelsLike, int Humidity);

public sealed record OpenWeatherWind(double Speed);

public sealed record OpenWeatherSys(string? Country);

public sealed record OpenWeatherCondition(string? Description, string? Icon);

// The error body is not formally documented; "message" is read defensively.
public sealed record OpenWeatherError(string? Message);
