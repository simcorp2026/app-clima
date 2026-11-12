namespace TiempoApp.Tests.Fakes;

internal static class SampleResponses
{
    // Trimmed real-shaped payload of /data/2.5/weather (Quito, UTC-5).
    public const string QuitoOk = """
        {
          "coord": { "lon": -78.5249, "lat": -0.2299 },
          "weather": [ { "id": 803, "main": "Clouds", "description": "nubes rotas", "icon": "04d" } ],
          "main": { "temp": 17.456, "feels_like": 16.94, "temp_min": 16.1, "temp_max": 18.2, "pressure": 1022, "humidity": 63 },
          "wind": { "speed": 3.6, "deg": 80 },
          "dt": 1762963200,
          "sys": { "country": "EC", "sunrise": 1762944921, "sunset": 1762988563 },
          "timezone": -18000,
          "name": "Quito",
          "cod": 200
        }
        """;

    public const string NotFound = """{ "cod": "404", "message": "city not found" }""";

    public const string Unauthorized = """{ "cod": 401, "message": "Invalid API key." }""";
}
