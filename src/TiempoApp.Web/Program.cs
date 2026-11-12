using Scalar.AspNetCore;
using TiempoApp.Core;
using TiempoApp.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWeatherCore(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseDefaultFiles();   // "/" -> wwwroot/index.html
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // /openapi/v1.json
    app.MapScalarApiReference(o =>    // /scalar
        o.WithTitle("TiempoApp API"));
}

app.MapHealthChecks("/health");
app.MapWeatherEndpoints();

app.Run();
