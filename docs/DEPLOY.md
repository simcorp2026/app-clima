# Despliegue de TiempoApp

Guía para publicar **TiempoApp.Web** (API + frontend) y distribuir **TiempoApp.Desktop**. Solo documentación.

## Principios comunes

| Tema | Regla |
|---|---|
| API key | Siempre como **variable de entorno o secreto del proveedor**: `OpenWeather__ApiKey` (el doble guion bajo equivale a `:`). Nunca en `appsettings.json`, en la imagen ni en el repositorio. |
| Entorno | `ASPNETCORE_ENVIRONMENT=Production` (valor por defecto de la imagen Docker). OpenAPI y Scalar quedan desactivados. |
| HTTPS | Obligatorio. Lo termina el proveedor o el proxy inverso; la app escucha HTTP dentro del contenedor. |
| Puerto | `8080` en el contenedor (`ASPNETCORE_HTTP_PORTS`). Si el proveedor impone otro, cambia esa variable. |
| Health check | `GET /health` → `200 Healthy`. |
| Cuota del plan gratuito | La caché de 10 min por ciudad (`OpenWeather__CacheMinutes`) reduce las llamadas. Con varias instancias, cada una tiene su propia caché en memoria. |

> Si la key falta, la app **no arranca** (validación de opciones al inicio): el fallo aparece en el despliegue, no en la primera petición.

## Contenedor (base de las opciones B y C)

El `Dockerfile` de la raíz construye solo la Web en dos etapas (`sdk:10.0` → `aspnet:10.0`), restaura con Central Package Management y ejecuta como el usuario no root `app` (uid 1654).

```bash
docker build -t tiempoapp-web .
```

Ejecutar en local sin escribir la key en el comando (queda fuera del historial de la shell):

```bash
# bash
export OpenWeather__ApiKey="<TU_API_KEY>"        # o léela de un gestor de secretos
docker run --rm -p 8080:8080 -e OpenWeather__ApiKey tiempoapp-web
```

```powershell
# PowerShell
$env:OpenWeather__ApiKey = "<TU_API_KEY>"
docker run --rm -p 8080:8080 -e OpenWeather__ApiKey tiempoapp-web
```

`-e OpenWeather__ApiKey` sin `=valor` copia la variable del entorno actual. Comprobar:

```bash
curl http://localhost:8080/health
curl "http://localhost:8080/api/weather?city=Quito"
```

**Detrás de un proxy** (todas las opciones en la nube):

- Al arrancar se verá `Failed to determine the https port for redirect`. Es esperado: el TLS lo termina el proxy y la redirección a HTTPS la hace la plataforma.
- Si necesitas que la app vea el esquema y la IP reales del cliente, añade `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`. Actívalo solo cuando el contenedor **no** sea accesible directamente desde Internet, porque confía en las cabeceras `X-Forwarded-*` de cualquier origen.

## Opción A: Azure App Service (la más directa para .NET)

Despliegue del código sin contenedor.

1. Crear el grupo de recursos, el plan (Linux, F1 o B1) y la Web App con runtime .NET 10:
   ```bash
   az group create -n rg-tiempoapp -l westeurope
   az appservice plan create -g rg-tiempoapp -n plan-tiempoapp --is-linux --sku B1
   az webapp create -g rg-tiempoapp -p plan-tiempoapp -n <app> --runtime "DOTNETCORE:10.0"
   ```
2. Definir la key en *Settings → Environment variables* como `OpenWeather__ApiKey`, o mejor como referencia a Key Vault: `@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/OpenWeatherApiKey/)` (requiere identidad administrada con permiso de lectura de secretos).
3. Publicar:
   ```bash
   dotnet publish src/TiempoApp.Web -c Release -o publish
   # comprimir el CONTENIDO de publish/ (no la carpeta) en publish.zip
   az webapp deploy -g rg-tiempoapp -n <app> --src-path publish.zip --type zip
   ```
   O desde Visual Studio: *Build → Publish → Azure → Azure App Service (Linux)*.
4. Activar *HTTPS Only* y configurar *Monitoring → Health check* con la ruta `/health`.
5. (Opcional) CI/CD con GitHub Actions: `actions/setup-dotnet` + `dotnet publish` + `azure/webapps-deploy`, autenticando con OIDC (`azure/login`) en lugar de guardar un *publish profile*.

## Opción B: contenedor gestionado (Azure Container Apps / Google Cloud Run / AWS App Runner)

1. Subir la imagen a un registro:

   | Proveedor | Registro | Ejemplo |
   |---|---|---|
   | Azure | ACR | `az acr build -r <registro> -t tiempoapp-web:1.0 .` |
   | Google | Artifact Registry | `gcloud builds submit --tag <región>-docker.pkg.dev/<proyecto>/<repo>/tiempoapp-web:1.0` |
   | AWS | ECR | `docker build` + `docker tag` + `docker push <cuenta>.dkr.ecr.<región>.amazonaws.com/tiempoapp-web:1.0` |

2. Crear el servicio apuntando a la imagen, con puerto **8080** y `OpenWeather__ApiKey` como **secreto** del servicio (no como variable en texto plano):
   - **Container Apps**: `az containerapp create ... --target-port 8080 --ingress external --secrets owm-key=<valor> --env-vars OpenWeather__ApiKey=secretref:owm-key`, o una referencia a Key Vault.
   - **Cloud Run**: guardar la key en Secret Manager y `gcloud run deploy tiempoapp --image <imagen> --port 8080 --set-secrets OpenWeather__ApiKey=owm-key:latest`.
   - **App Runner**: puerto 8080 y la key como *runtime environment secret* desde Secrets Manager o Parameter Store.
3. Health check HTTP en `/health`.
4. Escala mínima **0** para un coste casi nulo en una demo, aceptando el arranque en frío (unos segundos en la primera petición). App Runner no escala a 0; puede pausarse.
5. Dominio personalizado con certificado administrado si el cliente lo requiere.

## Opción C: VPS con Docker Compose (control total, bajo coste)

En el servidor, junto a la imagen (construida allí o descargada de un registro):

`.env`, **fuera del repositorio** y con permisos restringidos (`chmod 600 .env`):

```dotenv
OpenWeather__ApiKey=<TU_API_KEY>
```

`compose.yaml`:

```yaml
services:
  web:
    image: tiempoapp-web:1.0
    restart: unless-stopped
    env_file: .env
    environment:
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"   # solo Caddy llega al contenedor
    expose:
      - "8080"            # sin "ports": no se publica en el host

  caddy:
    image: caddy:2
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy_data:/data
    depends_on:
      - web

volumes:
  caddy_data:
```

`Caddyfile` (Caddy obtiene y renueva el certificado de Let's Encrypt automáticamente; el DNS del dominio debe apuntar al VPS):

```caddyfile
tiempo.example.com {
    reverse_proxy web:8080
}
```

```bash
docker compose up -d
curl https://tiempo.example.com/health
```

Con Nginx el esquema es el mismo: `proxy_pass http://web:8080;` con las cabeceras `X-Forwarded-For` y `X-Forwarded-Proto`, y certificados con Certbot.

## Aplicación de escritorio

La app WPF **no se despliega en la nube**: se distribuye como ejecutable para Windows.

### Publicar

```bash
dotnet publish src/TiempoApp.Desktop -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist/desktop
```

- Genera `TiempoApp.Desktop.exe` (unos 3 MB) junto a `appsettings.json`, que se deja fuera del `.exe` a propósito para poder cambiar el modo y la URL sin recompilar. Los `.pdb` pueden omitirse con `-p:DebugType=none`.
- `--self-contained false` requiere que el cliente tenga instalado el **.NET 10 Desktop Runtime**. Si no puede instalarlo, usa `--self-contained true` (incluye el runtime; el ejecutable pasa a ocupar decenas de MB).

### Configurar para clientes: siempre modo `Api`

Una app de escritorio no puede proteger una API key: cualquiera podría extraerla del ejecutable o de su configuración. Por eso, antes de entregar el `.exe` a terceros, edita el `appsettings.json` publicado:

```json
"WeatherSource": {
  "Mode": "Api",
  "ApiBaseUrl": "https://<tu-app>/"
}
```

En modo `Api` el escritorio solo habla con `TiempoApp.Web` desplegada y no necesita ninguna key. Verificado con el ejecutable publicado contra la imagen Docker en local.

### Mejoras futuras

- **Firmar** el ejecutable (certificado de firma de código) para evitar el aviso de SmartScreen.
- **Instalador** MSIX o Inno Setup, con accesos directos y actualizaciones.

## Checklist previo a cualquier despliegue

- [ ] Sin secretos en el repositorio ni en la imagen (`git grep -i apikey`, `docker image inspect`).
- [ ] `OpenWeather__ApiKey` definida como secreto del proveedor.
- [ ] `ASPNETCORE_ENVIRONMENT=Production`: `/scalar` y `/openapi/v1.json` devuelven 404.
- [ ] HTTPS activo y redirección de HTTP a HTTPS en la plataforma o el proxy.
- [ ] `/health` responde 200 y está configurado como health check.
- [ ] Caché activa (`CacheMinutes` > 0) y límites del plan gratuito de OpenWeatherMap tenidos en cuenta.
- [ ] Ejecutable de escritorio para clientes configurado en modo `Api` con la URL pública.
