using TiempoApp.Core.Models;

namespace TiempoApp.Core.Services;

public interface IWeatherService
{
    /// <exception cref="WeatherServiceException">The weather could not be retrieved.</exception>
    Task<WeatherDto> GetCurrentAsync(string city, CancellationToken cancellationToken = default);
}
