namespace TiempoApp.Core.Services;

public enum WeatherErrorKind
{
    CityNotFound,
    InvalidApiKey,
    RateLimited,
    Timeout,
    Unavailable,
    InvalidResponse,
}

/// <summary>UI-agnostic error: Web maps it to ProblemDetails, Desktop to a friendly message.</summary>
public sealed class WeatherServiceException(WeatherErrorKind kind, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public WeatherErrorKind Kind { get; } = kind;
}
