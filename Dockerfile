# syntax=docker/dockerfile:1
# TiempoApp.Web: multi-stage build (SDK -> ASP.NET runtime), runs as non-root on port 8080.
# Build from the repository root:  docker build -t tiempoapp-web .
# The API key is NEVER baked into the image: pass OpenWeather__ApiKey at run time.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Restore with only the project/props files so this layer is cached between code changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/TiempoApp.Core/TiempoApp.Core.csproj src/TiempoApp.Core/
COPY src/TiempoApp.Web/TiempoApp.Web.csproj src/TiempoApp.Web/
RUN dotnet restore src/TiempoApp.Web/TiempoApp.Web.csproj

# 2) Copy the sources and publish.
COPY src/TiempoApp.Core/ src/TiempoApp.Core/
COPY src/TiempoApp.Web/ src/TiempoApp.Web/
RUN dotnet publish src/TiempoApp.Web/TiempoApp.Web.csproj -c Release -o /app/publish \
    --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# Non-root user provided by the official .NET images.
USER $APP_UID
ENTRYPOINT ["dotnet", "TiempoApp.Web.dll"]
