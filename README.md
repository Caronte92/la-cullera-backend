# La Cullera — Backend

Backend en .NET 9 para La Cullera, con arquitectura en capas (Domain / Application / Infrastructure / Api), PostgreSQL, Redis y Docker.

## Requisitos previos

- **.NET 9 SDK** — [Descargar](https://dotnet.microsoft.com/download)
- **Docker** y **Docker Compose** — [Descargar](https://www.docker.com/products/docker-desktop)
- **Git**
- (Opcional) `dotnet-ef` como herramienta global, para gestionar migraciones localmente:

```bash
dotnet tool install --global dotnet-ef --version 9.0.0
export PATH="$PATH:$HOME/.dotnet/tools"
```

Verificar instalación:

```bash
dotnet --version
docker --version
docker compose version
```

## Estructura del proyecto

```text
la-cullera-backend/
├── Domain/                # Entidades y lógica de negocio
├── Application/           # Casos de uso, interfaces, DTOs, validadores
├── Infrastructure/        # EF Core, repositorios, servicios, migraciones
├── Api.Public/            # API pública (Minimal APIs)
├── Api.Admin/             # API administrativa (Minimal APIs)
├── *.Tests/                # Application.Tests, Api.Public.Tests, Api.Admin.Tests
├── nginx/                 # Configuración de reverse proxy (producción)
├── certbot/                # Certificados Let's Encrypt (producción)
├── Dockerfile              # Multi-stage build
├── docker-compose.yml
└── start-dev.sh            # Script de arranque rápido en local
```

## Arquitectura

```text
Request → Api (Program.cs)
        ↓
     Application (casos de uso)
        ↓
  Infrastructure (datos / servicios)
        ↓
      Domain (entidades)
```

Patrones: Repository genérico (`IRepository<T>` / `Repository<T>`), Dependency Injection nativa de .NET, EF Core Code-First, Minimal APIs.

## Variables de entorno

No hay credenciales por defecto en `docker-compose.yml`: todas las variables (`DB_USER`, `DB_PASSWORD`, `JWT_SECRET`, etc.) se leen del entorno y son obligatorias para levantar los servicios.

El proyecto usa dos ficheros de entorno, **excluidos de git** (`.env*` en `.gitignore`):

- `.env.dev` — desarrollo local
- `.env.pro` — producción

Variables requeridas:

```env
ASPNETCORE_ENVIRONMENT=Development|Production

JWT_SECRET=...            # genera uno con: openssl rand -base64 48
JWT_EXPIRY_MINUTES=1440

DB_HOST=db
DB_PORT=5432
DB_NAME=...
DB_USER=...
DB_PASSWORD=...

REDIS_CONNECTION=redis_cache:6379
Cors__Origin=http://localhost:3000
```

En producción, gestiona estos valores con un gestor de secretos (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) o las variables de entorno del propio despliegue. Nunca comitees `.env.dev`, `.env.pro` ni ningún fichero con secretos reales.

## Arranque rápido

### Opción 1 — Script de desarrollo (recomendado)

```bash
./start-dev.sh
```

Levanta `db`, `redis`, `api_public` y `api_admin` con `.env.dev`, espera a que Postgres esté healthy y comprueba que las APIs respondan.

### Opción 2 — Docker Compose manual

```bash
docker compose --env-file .env.dev up --build
```

| Servicio   | URL / Puerto            |
|------------|-------------------------|
| API Public | `http://localhost:9000` |
| API Admin  | `http://localhost:9001` |
| PostgreSQL | `localhost:5432`        |
| Redis      | `localhost:6379`        |

Aplicar migraciones si no se aplican automáticamente al arrancar:

```bash
dotnet ef database update --project Infrastructure --startup-project Api.Public
```

### Opción 3 — Ejecución local sin Docker para las APIs

Requiere .NET 9 SDK, y PostgreSQL/Redis corriendo (localmente o vía Docker):

```bash
docker compose --env-file .env.dev up db redis -d
dotnet run --project Api.Public
dotnet run --project Api.Admin   # opcional
```

## Migraciones de EF Core

```bash
# Crear migración
dotnet ef migrations add NombreDeLaMigracion --project Infrastructure --startup-project Api.Public

# Aplicar migraciones
dotnet ef database update --project Infrastructure --startup-project Api.Public

# Revertir a una migración anterior
dotnet ef database update NombreMigracionAnterior --project Infrastructure --startup-project Api.Public
```

## Tests

```bash
dotnet test
dotnet test Application.Tests
dotnet test Api.Public.Tests
```

## Calidad de código

- **Roslyn Analyzers** (`Microsoft.CodeAnalysis.NetAnalyzers`) y **Roslynator** — análisis estático.
- **StyleCop Analyzers** — convenciones de estilo (configuradas en `.editorconfig`).
- **dotnet-format** — formateo automático.
- **Husky.Net** — hook de pre-commit (`.husky/pre-commit`) que ejecuta restauración de herramientas, `dotnet format --verify-no-changes` y `dotnet test`.

```bash
dotnet format --verify-no-changes   # verificar sin aplicar cambios (usado en CI)
dotnet format                       # aplicar formato
dotnet tool restore                 # restaurar herramientas locales
```

Para saltarte el hook puntualmente: `git commit --no-verify`.

## Docker

```bash
docker build -t la-cullera-backend:latest .
docker compose logs -f api_public
docker compose logs -f api_admin
docker compose down          # detener servicios
docker compose down -v       # detener y borrar volúmenes (limpia la BD)
```

## Producción (nginx + Let's Encrypt)

`docker-compose.yml` incluye `nginx` y `certbot` para servir la API pública por HTTPS. El dominio y el email de contacto para el registro del certificado se configuran mediante variables de entorno (`CERTBOT_DOMAIN`, `CERTBOT_EMAIL`) — ver `init-letsencrypt.sh`.

## Troubleshooting

### "Connection string 'Default' not found"

Verifica que las variables `DB_*` están definidas en el `.env` usado y que PostgreSQL está corriendo.

### "The context cannot be used while the model is being created"

```bash
dotnet ef database drop --project Infrastructure --startup-project Api.Public
dotnet ef database update --project Infrastructure --startup-project Api.Public
```

### Puerto ya en uso

Cambia el puerto en `docker-compose.yml` (o en `launchSettings.json` si ejecutas sin Docker).

### Docker sin permisos (Linux)

```bash
sudo usermod -aG docker $USER
```

## Buenas prácticas

- No comitees secretos ni ficheros `.env*` con credenciales reales.
- Usa un gestor de secretos en producción.
- Revisa el SQL generado por las migraciones antes de aplicarlo en producción.
- Añade validaciones (FluentValidation), logging estructurado (Serilog) y middleware de errores en la capa API.
