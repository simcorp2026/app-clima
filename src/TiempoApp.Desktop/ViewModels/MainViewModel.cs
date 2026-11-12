using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TiempoApp.Core.Services;

namespace TiempoApp.Desktop.ViewModels;

public enum ViewState
{
    Empty,
    Loading,
    Error,
    Success,
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(IWeatherService weatherService, IOptions<WeatherSourceOptions> source, ILogger<MainViewModel> logger)
    {
        _weatherService = weatherService;
        _logger = logger;

        City = string.Empty;
        ErrorTitle = string.Empty;
        ErrorDetail = string.Empty;
        SourceLabel = source.Value.Mode == WeatherSourceMode.Api
            ? $"Vía API TiempoApp ({source.Value.ApiBaseUrl?.Host})"
            : "Conexión directa a OpenWeatherMap";
    }

    [ObservableProperty]
    public partial string City { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(IsLoading), nameof(HasError), nameof(HasWeather))]
    public partial ViewState State { get; private set; }

    [ObservableProperty]
    public partial WeatherDisplay? Weather { get; private set; }

    [ObservableProperty]
    public partial string ErrorTitle { get; private set; }

    [ObservableProperty]
    public partial string ErrorDetail { get; private set; }

    public string SourceLabel { get; }

    /// <summary>Quick demo chips (same list as the web).</summary>
    public IReadOnlyList<string> SampleCities { get; } =
        ["Quito", "Bogotá", "Lima", "Ciudad de México", "Madrid", "Buenos Aires"];

    public bool IsEmpty => State == ViewState.Empty;
    public bool IsLoading => State == ViewState.Loading;
    public bool HasError => State == ViewState.Error;
    public bool HasWeather => State == ViewState.Success;

    /// <summary>Generates SearchCommand (disabled while running) and SearchCancelCommand.</summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        var city = City.Trim();
        if (city.Length == 0)
        {
            ShowError("Falta la ciudad", "Escribe el nombre de una ciudad.");
            return;
        }

        State = ViewState.Loading;
        try
        {
            var weather = await _weatherService.GetCurrentAsync(city, cancellationToken);
            Weather = WeatherDisplay.From(weather);
            State = ViewState.Success;
        }
        catch (WeatherServiceException ex)
        {
            LogSearchFailed(_logger, city, ex.Kind, ex);
            var (title, detail) = WeatherErrorMessages.Describe(ex.Kind, city);
            ShowError(title, detail);
        }
        catch (OperationCanceledException)
        {
            // User cancelled: go back to whatever was shown before.
            State = Weather is null ? ViewState.Empty : ViewState.Success;
        }
    }

    [RelayCommand]
    private async Task SearchCityAsync(string city)
    {
        if (SearchCommand.IsRunning)
        {
            return;
        }

        City = city;
        await SearchCommand.ExecuteAsync(null);
    }

    private void ShowError(string title, string detail)
    {
        ErrorTitle = title;
        ErrorDetail = detail;
        State = ViewState.Error;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Weather search for '{City}' failed ({Kind})")]
    private static partial void LogSearchFailed(ILogger logger, string city, WeatherErrorKind kind, Exception exception);
}
