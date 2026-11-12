# Prompt inicial: WeatherDemo (Aplicación del clima en C# / .NET)

> Pega este archivo como contexto inicial en Claude Code (Visual Studio 2026). Trabaja por fases, una a la vez, y espera mi confirmación antes de pasar a la siguiente.

## 1. Contexto y objetivo

Aplicación **demostrativa** para mostrar a posibles clientes. Consume una API meteorológica externa (OpenWeatherMap) con `HttpClient`, procesa el JSON y muestra el clima actual de una ciudad. Se entrega en **dos formatos que comparten la misma lógica**: una web local (ASP.NET Core) y una app de escritorio (WPF).

- Se ejecuta **en local** por defecto.
- Debe quedar **lista para desplegar en la nube** (instrucciones incluidas en la fase final, sin desplegar nada todavía).
- Prioridad: código limpio, legible, fácil de explicar en una demo. Sin sobreingeniería.

## 2. Perfil del desarrollador (cómo responder)

- Soy desarrollador full stack: no expliques conceptos básicos, ve directo al punto.
- Cuando te pida código o cambios, responde con este formato:
  - **Dónde:** ruta del archivo y ubicación exacta del cambio (clase, método o línea aproximada). Si es archivo nuevo, la ruta donde crearlo.
  - **Código:** solo el fragmento que cambia, con unas líneas de contexto antes y después. No reescribas el archivo completo salvo que sea nuevo o lo pida.
  - **Explicación:** 1 a 3 líneas: qué hace y por qué.
- Idioma: español (código, nombres de clases y comentarios técnicos en inglés está bien).

## 3. Stack tecnológico propuesto

| Capa | Elección | Motivo |
|---|---|---|
| IDE | Visual Studio 2026 | Requisito |
| Runtime | .NET 10 (LTS) | Versión LTS vigente |
| Lenguaje | C# 14 | Incluido con .NET 10 |
| Tipo de app | **ASP.NET Core Minimal API + frontend estático (HTML/CSS/JS) en `wwwroot`** | Un solo proyecto, un solo despliegue, ideal para demo. La API key queda en el servidor, nunca en el navegador |
| App de escritorio | **WPF (.NET 10) + MVVM con `CommunityToolkit.Mvvm`** | Windows-only, madura, soporte de primera en Visual Studio 2026 y diseñador XAML. Ideal para demo local |
| Código compartido | Librería `WeatherDemo.Core` (class library) | Typed client, DTOs, opciones y servicio de clima reutilizados por la web y el escritorio, sin duplicar lógica |
| Alternativas de escritorio (solo si el cliente lo pide) | .NET MAUI (multiplataforma) o Avalonia | No son el camino por defecto: más peso y complejidad para una demo |
| HTTP | `HttpClient` vía `IHttpClientFactory` (typed client) | Requisito + buenas prácticas |
| JSON | `System.Text.Json` con DTOs (`record`) | Nativo, sin dependencias |
| Resiliencia | `Microsoft.Extensions.Http.Resilience` (retry + timeout) | Evita fallos en demo por red inestable |
| Caché | `IMemoryCache` (10 min por ciudad) | Reduce llamadas y protege el límite del plan gratuito |
| Configuración | `appsettings.json` + **User Secrets** (local) + variables de entorno (nube) | La API key nunca se commitea |
| Logging | `ILogger` (consola) | Suficiente |
| Pruebas | xUnit + `HttpMessageHandler` falso | Probar el cliente sin llamar a la API real |
| Documentación API | OpenAPI integrado de .NET 10 (`AddOpenApi`) | Útil para mostrar en la demo |
| Contenedores | Dockerfile multi-stage | Despliegue portable |

### API externa

- **OpenWeatherMap – Current Weather Data**: `GET https://api.openweathermap.org/data/2.5/weather?q={ciudad}&appid={KEY}&units=metric&lang=es`
- Plan gratuito suficiente. Mapear solo lo necesario: ciudad, país, temperatura, sensación térmica, humedad, viento, descripción e icono.
- Manejar: ciudad no encontrada (404), key inválida (401), límite excedido (429), timeout y errores de red. Devolver errores con `ProblemDetails`.
- Verifica en la documentación oficial vigente el endpoint y los campos antes de implementar.

### Estructura de solución sugerida

```
WeatherDemo/
├─ WeatherDemo.sln
├─ src/
│  ├─ WeatherDemo.Core/                     (class library compartida)
│  │  ├─ Services/IWeatherService.cs
│  │  ├─ Services/OpenWeatherClient.cs      (typed HttpClient)
│  │  ├─ Models/OpenWeatherResponse.cs      (DTO crudo de la API)
│  │  ├─ Models/WeatherDto.cs               (modelo propio hacia las UIs)
│  │  ├─ Options/OpenWeatherOptions.cs
│  │  └─ ServiceCollectionExtensions.cs     (AddWeatherCore(...))
│  ├─ WeatherDemo.Web/                      (Minimal API + frontend)
│  │  ├─ Program.cs
│  │  ├─ Endpoints/WeatherEndpoints.cs
│  │  └─ wwwroot/ (index.html, app.js, styles.css)
│  └─ WeatherDemo.Desktop/                  (WPF, MVVM)
│     ├─ App.xaml / App.xaml.cs             (Host genérico + DI)
│     ├─ Views/MainWindow.xaml
│     ├─ ViewModels/MainViewModel.cs
│     └─ appsettings.json
├─ tests/
│  └─ WeatherDemo.Tests/
├─ Dockerfile
├─ .gitignore
├─ .graphifyignore
├─ CLAUDE.md
└─ README.md
```

## 4. Uso de graphify (ahorro de tokens)

Usamos **graphify** (skill de Claude Code que convierte el proyecto en un knowledge graph consultable) para no releer archivos completos en cada sesión.

La skill de graphify **ya está instalada** en esta máquina: no la reinstales. Solo verifica que responde (`graphify --help`) y, si `/graphify` no se reconoce, reinicia la sesión de Claude Code.

Uso en el proyecto:

1. Dentro de Claude Code, en la raíz: `/graphify .` → genera `graphify-out/` con `GRAPH_REPORT.md`, `graph.json` y `graph.html`.
2. Activar integración permanente: `graphify claude install` (añade reglas a `CLAUDE.md` y un hook que sugiere consultar el grafo antes de Glob/Grep).
3. Actualizar de forma incremental tras cambios relevantes: `/graphify . --update`.

Reglas para ti (Claude) en este proyecto:

- Antes de responder preguntas de arquitectura o buscar código, **lee `graphify-out/GRAPH_REPORT.md`** y consulta el grafo en lugar de leer archivos completos.
- Lee archivos fuente completos solo cuando vayas a editarlos.
- Crea `.graphifyignore` con: `bin/`, `obj/`, `.vs/`, `node_modules/`, `graphify-out/`, `TestResults/`, `*.user`.
- Si `graphify` no está instalado o falla, dímelo y continúa sin él; no bloquees el trabajo.
- Mantén `CLAUDE.md` corto: stack, convenciones, comandos de build/test y la regla del grafo.

## 5. Convenciones de código

- `nullable` habilitado, `ImplicitUsings` habilitado, warnings como errores en Release.
- DTOs como `record`; inyección de dependencias por constructor; `async/await` con `CancellationToken`.
- Nada de secretos en el repo. `appsettings.json` lleva solo valores no sensibles (`BaseUrl`, `Units`, `Lang`, `CacheMinutes`).
- 
## 6. Fases de desarrollo

> Trabaja una fase a la vez. Al terminar cada una: resume qué hiciste en 2-3 líneas, indica cómo probarla y espera mi OK.

### Fase 0: Preparación
- Verificar `dotnet --info`, Git y (opcional) Docker.
- Crear la solución con proyectos `WeatherDemo.Core`, `WeatherDemo.Web`, `WeatherDemo.Desktop` (solo esqueleto, la UI llega en la Fase 4) y `WeatherDemo.Tests`; `.gitignore` (plantilla .NET), `CLAUDE.md` mínimo, `.graphifyignore`.
- Configurar User Secrets: `dotnet user-secrets set "OpenWeather:ApiKey" "<KEY>"`.
- **Entregable:** solución compila y arranca con un endpoint `/health`.

### Fase 1: Cliente de la API (núcleo)
- Todo en `WeatherDemo.Core`, expuesto con `AddWeatherCore(...)` para que Web y Desktop lo reutilicen.
- `OpenWeatherOptions` con validación al arrancar (`ValidateOnStart`).
- `OpenWeatherClient` como typed client + DTO crudo + mapeo a `WeatherDto`.
- Manejo de errores 401/404/429/timeouts.
- **Entregable:** `GET /api/weather?city=Quito` devuelve el JSON propio.

### Fase 2: Resiliencia, caché y pruebas
- Retry con backoff + timeout (resilience handler).
- `IMemoryCache` por ciudad (normalizar clave: minúsculas y trim).
- Pruebas xUnit del cliente con handler falso (éxito, 404, 401, JSON inválido).
- **Entregable:** `dotnet test` en verde.

### Fase 3: Interfaz de usuario
- Página estática en `wwwroot`: campo de ciudad, botón, tarjeta con temperatura, icono, humedad, viento y descripción; estados de carga y error.
- Diseño limpio, responsive, presentable para clientes (usar la skill/guía de diseño disponible si aplica).
- **Entregable:** demo completa funcionando en `https://localhost:xxxx`.

### Fase 4: App de escritorio (WPF)
- Crear `WeatherDemo.Desktop` (WPF, .NET 10) con Host genérico + DI, referenciando `WeatherDemo.Core`.
- `MainViewModel` con `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]` async con cancelación): ciudad, estado de carga, error y `WeatherDto` actual.
- `MainWindow.xaml`: mismo contenido que la web (ciudad, botón, tarjeta con temperatura, icono, humedad, viento, descripción), con estados de carga y error. Estilo coherente con la web (colores y tipografía).
- Modo de datos configurable en `appsettings.json`: `"Mode": "Direct"` (usa `WeatherDemo.Core` y la API de OpenWeatherMap con User Secrets) o `"Mode": "Api"` (llama a `WeatherDemo.Web` local o desplegada, sin exponer la API key en el cliente).
- **Importante:** una app de escritorio distribuida a clientes no puede guardar la API key de forma segura. Para uso local de demo basta el modo `Direct`; para entregar el `.exe` a terceros, usar el modo `Api`.
- Configurar proyectos de inicio múltiples en la solución (Web + Desktop) para la demo.
- **Entregable:** ambas apps muestran el clima de la misma ciudad, compartiendo `WeatherDemo.Core`.

### Fase 5: Pulido para demo
- OpenAPI/Scalar o Swagger UI visible solo en Development.
- Logging útil, mensajes de error amigables, ciudades de ejemplo rápidas (chips) en web y escritorio.
- `README.md` con: requisitos, cómo configurar la API key, cómo ejecutar web y escritorio, capturas (yo hago las capturas).
- Actualizar el grafo con graphify (`/graphify . --update`) y verificar que `GRAPH_REPORT.md` es coherente.

### Fase 6: Contenedor, publicación del escritorio y guía de despliegue en la nube
- `Dockerfile` multi-stage (sdk → aspnet), usuario no root, puerto 8080. Solo para `WeatherDemo.Web`.
- Verificar `docker build` y `docker run` en local con `-e OpenWeather__ApiKey=...`.
- Documentar la publicación del escritorio: `dotnet publish src/WeatherDemo.Desktop -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true` (o self-contained si el cliente no tiene el runtime). Indicar que firmar el ejecutable y un instalador (MSIX o Inno Setup) quedan como mejora futura.
- Redactar `docs/DEPLOY.md` (**solo documentación, no desplegar**) con las opciones de abajo.

## 7. Instrucciones de despliegue en la nube (contenido para `docs/DEPLOY.md`)

Principios comunes: la API key va como **variable de entorno / secreto del proveedor** (`OpenWeather__ApiKey`, el doble guion bajo equivale a `:`), HTTPS obligatorio, health check en `/health`, y la app escucha en el puerto que indique el proveedor.

### Opción A: Azure App Service (la más directa para .NET)
1. Crear un Resource Group y un App Service Plan (Linux, F1/B1) y un Web App con runtime .NET 10.
2. En *Settings → Environment variables* añadir `OpenWeather__ApiKey` (o referenciar Azure Key Vault).
3. Publicar desde Visual Studio (*Build → Publish → Azure*) o con CLI:
   `dotnet publish -c Release -o publish` y `az webapp deploy --resource-group <rg> --name <app> --src-path publish.zip --type zip`.
4. Configurar el health check en `/health` y verificar que HTTPS Only está activo.
5. (Opcional) CI/CD con GitHub Actions usando `azure/webapps-deploy`.

### Opción B: Contenedor en Azure Container Apps / Google Cloud Run / AWS App Runner
1. Construir y subir la imagen a un registro (ACR, Artifact Registry o ECR).
2. Crear el servicio apuntando a la imagen, puerto 8080, y definir `OpenWeather__ApiKey` como **secreto** del servicio.
3. Escala mínima 0 para costo casi nulo en demo (aceptando el arranque en frío).
4. Asignar dominio personalizado y certificado administrado si el cliente lo requiere.

### Opción C: VPS / Docker Compose (control total, bajo costo)
1. `docker compose up -d` con un `.env` fuera del repo.
2. Reverse proxy (Caddy o Nginx) con certificado Let's Encrypt.

Nota sobre el escritorio: la app WPF no se despliega en la nube. Tras desplegar la web, el escritorio se configura con `"Mode": "Api"` y `"ApiBaseUrl": "https://<tu-app>"` para consumirla.

Checklist previo a cualquier despliegue: sin secretos en el repo, `ASPNETCORE_ENVIRONMENT=Production`, OpenAPI deshabilitado en producción, límites de la API gratuita considerados (caché activa), `/health` responde 200.

## 8. Primera tarea para Claude Code

1. Lee este archivo completo.
2. Confirma qué versión de .NET SDK hay instalada (`dotnet --list-sdks`) y si graphify está instalado (`graphify --help`).
3. Propón (sin escribir código aún) cualquier ajuste al stack o a las fases y espera mi confirmación.
4. Tras mi OK, ejecuta la **Fase 0**.