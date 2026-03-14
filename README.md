# Project Base Backend

Plantilla base para proyectos backend en .NET 9, pensada para arrancar rápido con PostgreSQL y Docker.

**Estado:** Plantilla lista — funciones principales implementadas (EF Core, migraciones, DI, patrón repository, Serilog, FluentValidation, JWT demo, Swagger, CI). Siguientes pasos recomendados: habilitar hashing seguro de contraseñas, refresh tokens, seeding de BD y pipeline CI/CD para despliegues.

## Requisitos

- .NET 9 SDK
- Docker
- Docker Compose
- (Opcional) `dotnet-ef` global tool para gestionar migraciones localmente

Instalar `dotnet-ef` (si no está instalado):

```bash
dotnet tool install --global dotnet-ef --version 9.0.0
export PATH="$PATH:$HOME/.dotnet/tools"
```

## Estructura principal

- `Api.Public/`, `Api.Admin/` - Proyectos de API (Minimal APIs)
- `Application/` - Lógica de aplicación, interfaces y casos de uso
- `Domain/` - Entidades y modelos de dominio (`BaseEntity`, `Entities/`)
- `Infrastructure/` - Persistencia (EF Core), implementaciones y migraciones
- `Infrastructure/Migrations/` - Migraciones EF Core (ya contiene la migración inicial)

## Variables de entorno

Usamos un archivo `.env` para variables de entorno locales y `appsettings.json` para configuración por entorno.

Se incluye `.env.example` con las variables necesarias. Nunca comitees un `.env` con secretos.

### Manejo de secrets

- En desarrollo usar `.env` (excluido por `.gitignore`) y `appsettings.Development.json`.
- En producción usar un gestor de secretos (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) o variables de entorno del entorno de despliegue.
- `appsettings.json` debe contener placeholders o referencias a variables de entorno. Evitar poner credenciales en el repositorio.

Ejemplo para `appsettings.json` (usar `Environment.GetEnvironmentVariable` o la configuración por defecto de ASP.NET):

```json
{
  "ConnectionStrings": {
    "Default": "Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
  }
}
```

## .env.example

Ver [`.env.example`](.env.example) para todas las variables requeridas.

## Arranque rápido

### Opción 1 — Con Docker (recomendado)

Solo necesitas Docker y Docker Compose instalados:

```bash
docker compose up --build
```

Esto levanta:

| Servicio       | URL / Puerto            |
|----------------|-------------------------|
| API Public     | http://localhost:9000   |
| API Admin      | http://localhost:9001   |
| PostgreSQL     | localhost:5432          |
| Redis          | localhost:6379          |

Las credenciales de desarrollo ya están configuradas en `docker-compose.yml` (usuario `postgres`, contraseña `postgres`, base `appdb`).

Para aplicar las migraciones de EF Core (si no se aplican automáticamente al arrancar):

```bash
dotnet tool install --global dotnet-ef --version 9.0.0
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef database update --project Infrastructure --startup-project Api.Public
```

### Opción 2 — Sin Docker (ejecución local)

Requisitos: .NET 9 SDK, PostgreSQL y Redis corriendo localmente.

1. Levanta solo las dependencias con Docker:

```bash
docker compose up db redis -d
```

2. Ejecuta la API Public:

```bash
dotnet run --project Api.Public
```

3. (Opcional) Ejecuta la API Admin:

```bash
dotnet run --project Api.Admin
```

> **Nota:** El `docker-compose.yml` ya incluye las credenciales de desarrollo embebidas, así que con `docker compose up --build` debería funcionar directamente sin necesidad de un archivo `.env`.

## Migraciones de EF Core

- Se creó una migración inicial `InitialCreate` en `Infrastructure/Migrations`.
- Si añades entidades, crear migración:

```bash
dotnet ef migrations add NombreDeLaMigracion --project Infrastructure --startup-project Api.Public
dotnet ef database update --project Infrastructure --startup-project Api.Public
```

## Script inicial de BD

Si prefieres un script SQL en lugar de migraciones automáticas, aquí hay un ejemplo mínimo para PostgreSQL (crea la tabla `Users` usada en la plantilla):

```sql
CREATE TABLE IF NOT EXISTS "Users" (
  "Id" uuid PRIMARY KEY,
  "Username" varchar(50) NOT NULL UNIQUE,
  "Email" varchar(255) NOT NULL UNIQUE,
  "PasswordHash" varchar(255) NOT NULL,
  "FirstName" varchar(100) NOT NULL,
  "LastName" varchar(100) NOT NULL,
  "IsActive" boolean NOT NULL DEFAULT true,
  "CreatedAt" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "CreatedBy" varchar(255),
  "UpdatedAt" timestamp without time zone,
  "UpdatedBy" varchar(255),
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "DeletedAt" timestamp without time zone,
  "DeletedBy" varchar(255)
);

CREATE INDEX IF NOT EXISTS IX_Users_IsDeleted ON "Users" ("IsDeleted");
CREATE INDEX IF NOT EXISTS IX_Users_CreatedAt ON "Users" ("CreatedAt");
```

Puedes colocar este script en `Infrastructure/DbScripts/init.sql` y ejecutarlo contra la base de datos si lo prefieres.

## BaseEntity y Repository Pattern

- `Domain/Common/BaseEntity.cs`: Entidad base con `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, etc.
- `Application/Interfaces/IRepository.cs`: Interfaz genérica `IRepository<T>` con métodos async para CRUD y consultas.
- `Infrastructure/Repositories/Repository.cs`: Implementación genérica usando `AppDbContext` y filtros de soft-delete.

Estos ya están implementados en el repo y registrados en `Infrastructure/ServiceCollectionExtensions.cs`.

## Uso rápido (local)

1. Levanta Docker: `docker compose up --build`.
2. Ejecuta migraciones si no se aplican automáticamente.
3. Accede a las APIs: API Public en `http://localhost:9000`, API Admin en `http://localhost:9001`.

## Tests

- Proyectos de test con xUnit están en `Application.Tests`, `Api.Public.Tests`, `Api.Admin.Tests`.
- Ejecutar todos los tests:

```bash
dotnet test
```

## Buenas prácticas

- No comitees secretos ni `.env` con credenciales.
- Usa un gestor de secretos en producción.
- Mantén migraciones en `Infrastructure/Migrations` y revisa los SQL generados antes de aplicar en producción.
- Añade validaciones, logging estructurado y middleware de errores en la capa API.

## Contacto / Contribución

Issues y PRs bienvenidos. Usa el branch `main` para la plantilla estable.

---
Plantilla generada y mantenida por el equipo del proyecto.
# Project Base Backend

Proyecto base para aplicaciones backend con arquitectura en capas usando .NET 9.0, PostgreSQL y Docker.

## 📋 Requisitos Previos

- **.NET 9.0 SDK** o superior - [Descargar](https://dotnet.microsoft.com/download)
- **Docker** y **Docker Compose** - [Descargar](https://www.docker.com/products/docker-desktop)
- **Git** - [Descargar](https://git-scm.com/downloads)

### Verificar instalación:
```bash
dotnet --version
docker --version
docker-compose --version
```

---

## 🚀 Inicio Rápido

### 1. Clonar el repositorio
```bash
git clone https://github.com/Caronte92/project-base-backend.git
cd project-base-backend
```

### 2. Configurar variables de entorno
```bash
cp .env.example .env
# Editar .env si es necesario
```

### 3. Levantar servicios con Docker Compose
```bash
docker-compose up -d
```

Esto levantará:
- **PostgreSQL 16** en `localhost:5432`
- **Api.Public** en `http://localhost:9000`
- **Api.Admin** en `http://localhost:9001`

### 4. Crear y aplicar migraciones (Primera vez)
```bash
dotnet ef database update --project Infrastructure --startup-project Api.Public
```

### 5. Compilar y ejecutar tests
```bash
dotnet build
dotnet test
```

---

## 📁 Estructura del Proyecto

```
proyecto-base-backend/
├── Domain/                      # Entidades y lógica de negocio
│   ├── Entities/               # Modelos de dominio
│   └── Domain.csproj
│
├── Application/                 # Lógica de aplicación
│   ├── Common/                 # Tipos comunes (HealthStatus, etc)
│   ├── Interfaces/             # Interfaces de aplicación
│   └── Application.csproj
│
├── Infrastructure/              # Implementación de persistencia
│   ├── Persistence/            # DbContext, configuraciones
│   ├── Repositories/           # Implementación de repositorios
│   ├── Services/               # Servicios de negocio
│   ├── Migrations/             # Migraciones de BD
│   └── Infrastructure.csproj
│
├── Api.Public/                  # API pública
│   ├── Program.cs              # Configuración
│   ├── appsettings.json
│   └── Api.Public.csproj
│
├── Api.Admin/                   # API administrativa
│   ├── Program.cs
│   ├── appsettings.json
│   └── Api.Admin.csproj
│
├── *Tests/                      # Proyectos de pruebas
│   ├── Application.Tests
│   ├── Api.Public.Tests
│   └── Api.Admin.Tests
│
├── Dockerfile                   # Multi-stage Docker build
├── docker-compose.yml           # Servicios Docker
├── Template.sln                 # Solución Visual Studio
└── README.md
```

---

## 🏗️ Arquitectura

### Capas

1. **Domain** - Entidades y contratos puros (sin dependencias)
2. **Application** - Casos de uso y lógica de aplicación
3. **Infrastructure** - Implementación de persistencia, servicios externos
4. **Api** - Endpoints REST, configuración HTTP

### Patrones Implementados

- **Repository Pattern** - Acceso a datos genérico
- **Dependency Injection** - Inyección de dependencias nativa de .NET
- **Entity Framework Core** - ORM con Code-First
- **Minimal APIs** - Endpoints sin controladores

---

## 🔧 Configuración

### Variables de Entorno (`.env`)

```env
# Database
DB_HOST=db
DB_PORT=5432
DB_NAME=appdb
DB_USER=postgres
DB_PASSWORD=postgres

# API
API_ENVIRONMENT=Development
API_CORS_ORIGIN=http://localhost:3000
```

### Appsettings por Entorno

- `appsettings.json` - Configuración base
- `appsettings.Development.json` - Desarrollo (logs verbosos)
- `appsettings.Production.json` - Producción (optimizaciones)

---

## 🗄️ Base de Datos

### Crear una nueva migración

```bash
dotnet ef migrations add NombreMigracion --project Infrastructure --startup-project Api.Public
```

### Aplicar migraciones

```bash
dotnet ef database update --project Infrastructure --startup-project Api.Public
```

### Revertir última migración

```bash
dotnet ef database update NombreMigracionAnterior --project Infrastructure --startup-project Api.Public
```

---

## 🧪 Testing

### Ejecutar todos los tests
```bash
dotnet test
```

### Ejecutar tests de un proyecto específico
```bash
dotnet test Application.Tests
dotnet test Api.Public.Tests
```

### Con cobertura de código
```bash
dotnet test /p:CollectCoverageMetrics=true
```

### Proyectos de prueba incluidos
- **Application.Tests** - Tests unitarios de lógica
- **Api.Public.Tests** - Tests de integración API pública
- **Api.Admin.Tests** - Tests de integración API admin

---

## 🎨 Calidad de Código

### Herramientas Incluidas

1. **Roslyn Analyzers** (`Microsoft.CodeAnalysis.NetAnalyzers`) - Análisis estático y detección de smells
2. **StyleCop Analyzers** - Convenciones de estilo de código C#
3. **Roslynator Analyzers** - Análisis adicional y refactorings
4. **dotnet-format** - Formateo automático de código
5. **Husky.Net** - Git hooks para validación pre-commit

### Configuración

- `.editorconfig` - Reglas de estilo compartidas (indentación, espacios, etc.)
- `Directory.Build.props` - Paquetes de análisis aplicados globalmente
- `.husky/pre-commit` - Hook que ejecuta formateo y tests antes de commit
- `.config/dotnet-tools.json` - Declaración de herramientas locales

### Uso de Herramientas

#### Verificar formato
```bash
# Verificar sin hacer cambios (usado en CI)
dotnet format --verify-no-changes

# Aplicar formato automáticamente
dotnet format
```

#### Instalar herramientas locales
```bash
# Restaurar herramientas definidas en .config/dotnet-tools.json
dotnet tool restore
```

#### Ejecutar análisis
El análisis ocurre automáticamente durante el build. Para ver detalles:
```bash
dotnet build --verbosity diagnostic
```

#### Pre-commit Hook
El hook automático ejecuta:
1. Restauración de herramientas (`dotnet tool restore`)
2. Verificación de formato (`dotnet format --verify-no-changes`)
3. Tests (`dotnet test`)

Para deshabilitar temporalmente:
```bash
git commit --no-verify
```

#### Severidades de Reglas
Las reglas de StyleCop se configuran en `.editorconfig`. Ejemplos:
- `SA1200` - Desactivado (incompatible con top-level statements de C# 10+)
- `SA1633` - Desactivado (file headers)
- `SA1611` - Desactivado (documentación de parámetros en overloads)

---

## 🐳 Docker

### Construir imagen
```bash
docker build -t project-base-backend:latest .
```

### Ejecutar contenedor individual
```bash
# API Pública
docker run -p 9000:8080 \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;..." \
  project-base-backend:api-public

# API Admin
docker run -p 9001:8080 \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;..." \
  project-base-backend:api-admin
```

### Ver logs
```bash
docker-compose logs -f api_public
docker-compose logs -f api_admin
docker-compose logs -f db
```

### Detener servicios
```bash
docker-compose down
```

### Detener y eliminar volúmenes (limpia BD)
```bash
docker-compose down -v
```

---

## 📚 Endpoints Disponibles

### Health Check
```
GET /health
```

Respuesta:
```json
{
  "status": "healthy",
  "timestamp": "2026-01-14T10:30:00Z"
}
```

### OpenAPI/Swagger
Disponible en `/openapi/v1.json` (en desarrollo)

---

## 🔐 Seguridad

### Contraseñas por defecto (⚠️ Cambiar en Producción)
- PostgreSQL: `postgres`
- Usuario: `postgres`

### En Producción
- Cambiar todas las contraseñas
- Usar variables de entorno seguros
- Habilitar HTTPS
- Configurar CORS apropiadamente
- Implementar autenticación JWT

---

## 📝 Desarrollo

### Añadir nueva funcionalidad

1. Crear entidad en `Domain/Entities`
2. Crear interfaz en `Application/Interfaces`
3. Implementar en `Infrastructure/Services` o `Infrastructure/Repositories`
4. Crear migración de BD si es necesario
5. Crear endpoint en `Api.Public` o `Api.Admin`
6. Escribir tests en `*.Tests`

### Patrón de capas

```
Request → Api (Program.cs)
        ↓
     Application (Lógica)
        ↓
  Infrastructure (Datos/Servicios)
        ↓
      Domain (Entidades)
```

---

## 🐛 Troubleshooting

### Error: "Connection string 'Default' not found"
- Verificar `appsettings.json`
- Asegurar que PostgreSQL está corriendo

### Error de migraciones: "The context cannot be used while the model is being created"
```bash
# Limpiar y regenerar
dotnet ef database drop --project Infrastructure --startup-project Api.Public
dotnet ef database update --project Infrastructure --startup-project Api.Public
```

### Puerto ya en uso
```bash
# Cambiar puertos en docker-compose.yml
# Cambiar puertos en launchSettings.json
```

### Docker sin permiso
```bash
# En Linux, agregar usuario a grupo docker
sudo usermod -aG docker $USER
```

---

## 📦 Dependencias Principales

- **Microsoft.EntityFrameworkCore** (9.0.0) - ORM
- **Npgsql.EntityFrameworkCore.PostgreSQL** (9.0.0) - Driver PostgreSQL
- **xUnit** (2.9.2) - Framework de testing
- **Moq** (4.20.72) - Mocking para tests

---

## 🤝 Contribuir

1. Crear rama: `git checkout -b feature/nueva-funcionalidad`
2. Commit cambios: `git commit -am 'Agregar nueva funcionalidad'`
3. Push rama: `git push origin feature/nueva-funcionalidad`
4. Abrir Pull Request

---

## 📄 Licencia

[Especificar licencia]

---

## 📧 Contacto

[Información de contacto]

---

**Última actualización:** 14 de enero de 2026
