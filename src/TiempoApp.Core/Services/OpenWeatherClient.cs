using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TiempoApp.Core.Models;
using TiempoApp.Core.Options;

namespace TiempoApp.Core.Services;

/// <summary>Typed HttpClient for OpenWeatherMap "Current Weather Data".</summary>
public sealed partial class OpenWeatherClient(
    HttpClient httpClient,
    IOptions<OpenWeatherOptions> options,
    ILogger<OpenWeatherClient> logger) : IWeatherService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly OpenWeatherOptions _options = options.Value;

    public async Task<WeatherDto> GetCurrentAsync(string city, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        city = city.Trim();

        // Relative to BaseAddress. Query by city name is deprecated upstream but still supported.
        var requestUri = $"weather?q={Uri.EscapeDataString(city)}&appid={Uri.EscapeDataString(_options.ApiKey)}" +
                         $"&units={_options.Units}&lang={_options.Lang}";

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (Exception ex) when (ex is TimeoutRejectedException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogFailure(logger, city, "timeout", ex);
            throw new WeatherServiceException(WeatherErrorKind.Timeout, "The weather provider did not respond in time.", ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException)
        {
            LogFailure(logger, city, "network error", ex);
            throw new WeatherServiceException(WeatherErrorKind.Unavailable, "Could not reach the weather provider.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateHttpErrorAsync(response, city, cancellationToken);
            }

            OpenWeatherResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<OpenWeatherResponse>(JsonOptions, cancellationToken);
            }
            catch (JsonException ex)
            {
                LogFailure(logger, city, "invalid JSON", ex);
                throw new WeatherServiceException(WeatherErrorKind.InvalidResponse, "The weather provider returned an invalid response.", ex);
            }

            return Map(payload);
        }
    }

    private async Task<WeatherServiceException> CreateHttpErrorAsync(
        HttpResponseMessage response, string city, CancellationToken cancellationToken)
    {
        var providerMessage = await TryReadErrorMessageAsync(response, cancellationToken);
        LogHttpError(logger, (int)response.StatusCode, city, providerMessage);

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => new(WeatherErrorKind.CityNotFound, $"City '{city}' was not found."),
            HttpStatusCode.Unauthorized => new(WeatherErrorKind.InvalidApiKey, "The OpenWeatherMap API key is invalid or not active yet."),
            HttpStatusCode.TooManyRequests => new(WeatherErrorKind.RateLimited, "The OpenWeatherMap request limit was exceeded."),
            _ => new(WeatherErrorKind.Unavailable, $"The weather provider returned HTTP {(int)response.StatusCode}."),
        };
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<OpenWeatherError>(JsonOptions, cancellationToken);
            return error?.Message;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private WeatherDto Map(OpenWeatherResponse? payload)
    {
        var condition = payload?.Weather is { Count: > 0 } conditions ? conditions[0] : null;

        if (payload?.Main is null || string.IsNullOrWhiteSpace(payload.Name) || string.IsNullOrWhiteSpace(condition?.Icon))
        {
            throw new WeatherServiceException(WeatherErrorKind.InvalidResponse, "The weather provider returned incomplete data.");
        }

        return new WeatherDto(
            City: payload.Name,
            Country: payload.Sys?.Country ?? string.Empty,
            Temperature: Math.Round(payload.Main.Temp, 1),
            FeelsLike: Math.Round(payload.Main.FeelsLike, 1),
            Humidity: payload.Main.Humidity,
            WindSpeed: Math.Round(payload.Wind?.Speed ?? 0, 1),
            Description: condition.Description ?? string.Empty,
            Icon: condition.Icon,
            IconUrl: $"{_options.IconBaseUrl.TrimEnd('/')}/{condition.Icon}@2x.png",
            Units: _options.Units,
            ObservedAt: DateTimeOffset.FromUnixTimeSeconds(payload.Dt).ToOffset(TimeSpan.FromSeconds(payload.Timezone)));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "OpenWeather request for '{City}' failed: {Reason}")]
    private static partial void LogFailure(ILogger logger, string city, string reason, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OpenWeather returned {StatusCode} for '{City}': {ProviderMessage}")]
    private static partial void LogHttpError(ILogger logger, int statusCode, string city, string? providerMessage);
}
