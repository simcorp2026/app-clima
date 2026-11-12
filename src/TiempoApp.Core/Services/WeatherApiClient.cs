using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TiempoApp.Core.Models;

namespace TiempoApp.Core.Services;

/// <summary>
/// Typed HttpClient for our own TiempoApp.Web API (<c>GET api/weather?city=</c>).
/// Used by clients that must not hold the OpenWeatherMap key (e.g. the desktop app in "Api" mode).
/// </summary>
public sealed class WeatherApiClient(HttpClient httpClient) : IWeatherService
{
    public async Task<WeatherDto> GetCurrentAsync(string city, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        try
        {
            using var response = await httpClient.GetAsync(
                $"api/weather?city={Uri.EscapeDataString(city.Trim())}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new WeatherServiceException(MapStatus(response.StatusCode),
                    $"TiempoApp API returned HTTP {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<WeatherDto>(cancellationToken)
                   ?? throw new WeatherServiceException(WeatherErrorKind.InvalidResponse, "TiempoApp API returned an empty body.");
        }
        catch (Exception ex) when (ex is TimeoutRejectedException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new WeatherServiceException(WeatherErrorKind.Timeout, "TiempoApp API did not respond in time.", ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException)
        {
            throw new WeatherServiceException(WeatherErrorKind.Unavailable, "Could not reach TiempoApp API.", ex);
        }
        catch (JsonException ex)
        {
            throw new WeatherServiceException(WeatherErrorKind.InvalidResponse, "TiempoApp API returned invalid JSON.", ex);
        }
    }

    // Inverse of the mapping in TiempoApp.Web (WeatherEndpoints).
    private static WeatherErrorKind MapStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.NotFound => WeatherErrorKind.CityNotFound,
        HttpStatusCode.TooManyRequests => WeatherErrorKind.RateLimited,
        HttpStatusCode.GatewayTimeout => WeatherErrorKind.Timeout,
        HttpStatusCode.ServiceUnavailable => WeatherErrorKind.InvalidApiKey,
        _ => WeatherErrorKind.Unavailable,
    };
}
