using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using TiempoApp.Core.Models;
using TiempoApp.Core.Services;

namespace TiempoApp.Web.Endpoints;

public static class WeatherEndpoints
{
    private const int MaxCityLength = 100;

    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/weather")
            .WithTags("Weather")
            .MapGet("/", GetCurrentAsync)
            .WithName("GetCurrentWeather")
            .WithSummary("Clima actual de una ciudad")
            .WithDescription("Consulta OpenWeatherMap (con caché por ciudad) y devuelve el contrato propio WeatherDto.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        return app;
    }

    private static async Task<Results<Ok<WeatherDto>, ValidationProblem, ProblemHttpResult>> GetCurrentAsync(
        [Description("Ciudad, opcionalmente con país ISO: 'Quito' o 'Valencia,VE'.")] string? city,
        IWeatherService weatherService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(city) || city.Trim().Length > MaxCityLength)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["city"] = [$"Indica una ciudad (máximo {MaxCityLength} caracteres)."],
            });
        }

        try
        {
            return TypedResults.Ok(await weatherService.GetCurrentAsync(city, cancellationToken));
        }
        catch (WeatherServiceException ex)
        {
            return ToProblem(ex, city.Trim());
        }
    }

    // Maps provider errors to client-facing ProblemDetails (texts shared with Desktop via WeatherErrorMessages).
    private static ProblemHttpResult ToProblem(WeatherServiceException ex, string city)
    {
        var (title, detail) = WeatherErrorMessages.Describe(ex.Kind, city);
        return TypedResults.Problem(detail: detail, statusCode: StatusFor(ex.Kind), title: title);
    }

    private static int StatusFor(WeatherErrorKind kind) => kind switch
    {
        WeatherErrorKind.CityNotFound => StatusCodes.Status404NotFound,
        WeatherErrorKind.RateLimited => StatusCodes.Status429TooManyRequests,
        WeatherErrorKind.Timeout => StatusCodes.Status504GatewayTimeout,
        // An invalid API key is a server configuration problem, not the caller's fault.
        WeatherErrorKind.InvalidApiKey => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status502BadGateway,
    };
}
