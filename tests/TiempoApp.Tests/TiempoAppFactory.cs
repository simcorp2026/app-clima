using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TiempoApp.Tests;

/// <summary>Web host for tests: fake API key so options validation passes without User Secrets.</summary>
public sealed class TiempoAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("OpenWeather:ApiKey", "test-key");
}
