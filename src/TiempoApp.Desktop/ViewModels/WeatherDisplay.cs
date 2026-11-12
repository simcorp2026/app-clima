using System.Globalization;
using TiempoApp.Core.Models;

namespace TiempoApp.Desktop.ViewModels;

/// <summary>Display-ready texts for the weather card (same formatting rules as wwwroot/app.js).</summary>
public sealed record WeatherDisplay(
    string City,
    string Country,
    string LocalTime,
    string IconUrl,
    string Temperature,
    string Unit,
    string Description,
    string FeelsLike,
    string Humidity,
    string Wind)
{
    public static WeatherDisplay From(WeatherDto w)
    {
        var culture = CultureInfo.CurrentCulture;
        var (unit, wind) = w.Units switch
        {
            "imperial" => ("°F", $"{Math.Round(w.WindSpeed).ToString(culture)} mph"),
            "standard" => ("K", $"{w.WindSpeed.ToString("0.0", culture)} m/s"),
            _ => ("°C", $"{Math.Round(w.WindSpeed * 3.6).ToString(culture)} km/h"),
        };

        return new WeatherDisplay(
            City: w.City,
            Country: CountryName(w.Country),
            // ObservedAt keeps the city's offset, so this is the city's local time.
            LocalTime: $"Actualizado {w.ObservedAt.ToString("HH:mm", culture)} (hora local)",
            IconUrl: w.IconUrl,
            Temperature: Math.Round(w.Temperature).ToString(culture),
            Unit: unit,
            Description: Capitalize(w.Description, culture),
            FeelsLike: $"{Math.Round(w.FeelsLike).ToString(culture)} {unit}",
            Humidity: $"{w.Humidity.ToString(culture)} %",
            Wind: wind);
    }

    private static string CountryName(string code)
    {
        try
        {
            return string.IsNullOrEmpty(code) ? string.Empty : new RegionInfo(code).DisplayName;
        }
        catch (ArgumentException)
        {
            return code;
        }
    }

    private static string Capitalize(string text, CultureInfo culture) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], culture) + text[1..];
}
