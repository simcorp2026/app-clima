using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TiempoApp.Desktop.ViewModels;
using TiempoApp.Desktop.Views;

namespace TiempoApp.Desktop;

/// <summary>Composition root: Generic Host + DI, same configuration model as TiempoApp.Web.</summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        // Spanish UI texts (country names, number formatting) regardless of the OS language.
        var spanish = CultureInfo.GetCultureInfo("es-ES");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = spanish;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentUICulture = spanish;

        try
        {
            _host = BuildHost(e.Args);
            await _host.StartAsync(); // runs ValidateOnStart for OpenWeatherOptions in "Direct" mode
        }
        catch (Exception ex) when (ex is OptionsValidationException or InvalidOperationException)
        {
            MessageBox.Show(ex.Message, "TiempoApp: configuración incompleta", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        MainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static IHost BuildHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory, // appsettings.json next to the .exe
        });

        // Local demo in "Direct" mode: same UserSecretsId as TiempoApp.Web.
        builder.Configuration.AddUserSecrets<App>(optional: true);

        builder.Services.AddWeatherSource(builder.Configuration);
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Ocurrió un error inesperado:\n{e.Exception.Message}", "TiempoApp",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
