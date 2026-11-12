namespace TiempoApp.Core.Services;

/// <summary>User-facing (Spanish) texts for each error kind, shared by Web and Desktop.</summary>
public static class WeatherErrorMessages
{
    public static (string Title, string Detail) Describe(WeatherErrorKind kind, string city) => kind switch
    {
        WeatherErrorKind.CityNotFound => ("Ciudad no encontrada",
            $"No se encontró la ciudad '{city}'. Revisa el nombre o añade el país (p. ej. 'Quito,EC')."),
        WeatherErrorKind.RateLimited => ("Demasiadas consultas",
            "Se alcanzó el límite de consultas al proveedor del clima. Inténtalo en un minuto."),
        WeatherErrorKind.Timeout => ("Tiempo de espera agotado",
            "El proveedor del clima tardó demasiado en responder."),
        WeatherErrorKind.InvalidApiKey => ("Servicio no disponible",
            "El servicio del clima no está configurado correctamente."),
        _ => ("Proveedor no disponible",
            "No se pudo obtener el clima en este momento."),
    };
}
