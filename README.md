# Alerto Management API

API RESTful versionada en .NET 8 para la gestion de alertas civiles
georreferenciadas en Medellin. El proyecto combina autenticacion JWT,
persistencia real en PostgreSQL, cache con Redis, CRUD completo, interfaz web
basica, consulta meteorologica con Open-Meteo y confirmaciones ciudadanas.

## Informacion academica

| Campo | Detalle |
|---|---|
| Institucion | Politecnico Colombiano Jaime Isaza Cadavid |
| Facultad | Facultad de Ingenieria |
| Programa | Ingenieria Informatica |
| Asignatura | Computacion Orientada a Servicios |
| Docente | Andres Felipe Gonzalez Orozco |
| Estudiantes | Federico Bayer Cuartas - Rafael Estiven Uribe Alvarez |
| Repositorio | https://github.com/EstivenUribe/Alerto |

## Contexto funcional

Alerto resuelve una necesidad real de alertamiento temprano: registrar,
validar, aprobar, difundir y consultar alertas civiles sin perder trazabilidad.
La API no expone solo tablas; expone capacidades de negocio para operadores,
administradores, analistas, auditores, ciudadanos y clientes maquina a maquina.

Procesos soportados:

- Registro y consulta de alertas por zona geografica.
- Reporte ciudadano de nuevas alertas pendientes de revision.
- Aprobacion, rechazo, cancelacion y difusion de alertas.
- Borrado administrativo logico de alertas, sin destruir registros historicos.
- Confirmacion ciudadana de alertas aprobadas o difundidas.
- Consulta meteorologica por coordenadas con persistencia de lecturas.
- Creacion automatica de alertas cuando el riesgo por lluvia es alto o critico.
- Administracion de usuarios, roles y geocercas.
- Auditoria de acciones criticas.

## Principios SOA aplicados

- Contrato versionado: rutas bajo `/api/v1/`.
- Interoperabilidad: HTTP, JSON, JWT Bearer, ProblemDetails y Swagger/OpenAPI.
- Desacoplamiento: Clean Architecture con puertos e implementaciones separadas.
- Stateless: cada request se valida mediante token JWT.
- Reutilizacion: la misma API sirve a la interfaz web, Postman y clientes M2M.
- Gobernanza: logs, health checks, rate limiting, auditoria y manual tecnico.

## Arquitectura

```mermaid
flowchart TB
    Client["Cliente web / Postman / M2M"]
    Api["Alerto.Api\nControllers, JWT, Swagger, Middlewares"]
    App["Alerto.Application\nCasos de uso, DTOs, Validadores, Puertos"]
    Domain["Alerto.Domain\nEntidades, Enums, Reglas"]
    Infra["Alerto.Infrastructure\nEF Core, Redis, JWT, Open-Meteo"]
    Pg[("PostgreSQL\nschema alerto")]
    Redis[("Redis")]
    Meteo["Open-Meteo"]

    Client -->|"HTTP + JSON + Bearer"| Api
    Api --> App
    App --> Domain
    Infra --> App
    Infra --> Domain
    Api --> Infra
    Infra --> Pg
    Infra --> Redis
    Infra --> Meteo
```

## Estructura del proyecto

```text
Alerto.sln
src/
  Alerto.Api/              API HTTP, Swagger, seguridad, frontend estatico
  Alerto.Application/      Casos de uso, DTOs, validadores, puertos
  Alerto.Domain/           Entidades, enums, value objects, reglas
  Alerto.Infrastructure/   EF Core, PostgreSQL, Redis, JWT, Open-Meteo
tests/
  Alerto.DomainTests/
  Alerto.ArchitectureTests/
  Alerto.IntegrationTests/
Coleccion de Postman.postman_collection.json
CheckPoint 3. 28.04.26.md
docker-compose.yml
```

## Tecnologias

| Tecnologia | Uso |
|---|---|
| .NET 8 / ASP.NET Core | API HTTP, DI, middlewares, seguridad |
| Entity Framework Core 8 | ORM, migraciones y concurrencia optimista |
| PostgreSQL 16 | Base de datos relacional principal |
| Redis 7 | Cache, locks e idempotencia |
| JWT Bearer | Autenticacion stateless |
| Refresh tokens | Renovacion controlada de sesiones |
| TOTP | Segundo factor para usuarios administrativos |
| FluentValidation | Validacion de requests |
| AutoMapper | Mapeo entre entidades y DTOs |
| Serilog | Logging estructurado |
| Polly | Resiliencia para clientes HTTP externos |
| Open-Meteo | Datos meteorologicos reales |
| HTML, CSS y JavaScript | Interfaz basica conectada a la API |
| Leaflet | Mapas en la interfaz web |
| Assets institucionales | Logo Alerto e imagen de Facultad de Ingenieria en login y pie de pagina |
| xUnit | Pruebas de dominio, arquitectura e integracion |
| Docker Compose | PostgreSQL y Redis en desarrollo |

## Requisitos previos

- .NET SDK 8
- Docker Desktop o motor Docker compatible
- Puerto `5433` disponible para PostgreSQL
- Puerto `6379` disponible para Redis
- Navegador web
- Postman, Thunder Client o cliente HTTP equivalente

## Ejecucion local

1. Levantar dependencias:

```bash
docker compose up -d
```

2. Restaurar y compilar:

```bash
dotnet restore Alerto.sln
dotnet build Alerto.sln
```

3. Ejecutar la API:

```bash
dotnet run --project src/Alerto.Api/Alerto.Api.csproj --launch-profile http
```

4. Abrir la interfaz web:

```text
http://localhost:5070/
```

5. Abrir Swagger:

```text
http://localhost:5070/swagger
```

La URL exacta puede variar segun el perfil de ejecucion mostrado por la
consola.

## Configuracion principal

La configuracion base esta en `src/Alerto.Api/appsettings.json` y puede
sobrescribirse con variables de entorno.

```bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__AlertoDb=Host=localhost;Port=5433;Database=alerto_db;Username=postgres;Password=postgres
ConnectionStrings__Redis=localhost:6379,abortConnect=false
Jwt__Issuer=alerto-api
Jwt__Audience=alerto-clients
Jwt__SecretKey=Alerto.Super.Secret.Key.For.DotNet.8.Api.2026
BootstrapAdmin__Username=admin
BootstrapAdmin__DisplayName=Administrador Alerto
BootstrapAdmin__Email=admin@alerto.local
BootstrapAdmin__Password=AlertoAdmin123!
Integrations__OpenMeteo__BaseUrl=https://api.open-meteo.com/
```

## Base de datos y migraciones

Al iniciar, la API aplica migraciones pendientes mediante `AlertoDbInitializer`.
Tambien verifica y crea datos demo cuando hacen falta:

- usuario administrador `admin`;
- usuario operador `operador`;
- usuario ciudadano `ciudadano`;
- geocerca de referencia;
- esquema `alerto` con tablas de usuarios, alertas, geocercas, auditoria,
  refresh tokens, outbox, lecturas meteorologicas y confirmaciones ciudadanas.

Comandos utiles:

```bash
dotnet ef migrations add NombreMigracion --project src/Alerto.Infrastructure --startup-project src/Alerto.Api
dotnet ef database update --project src/Alerto.Infrastructure --startup-project src/Alerto.Api
```

## Credenciales de prueba

Usuarios demo:

| Rol | Usuario | Password |
|---|---|---|
| Admin | `admin` | `AlertoAdmin123!` |
| Operator | `operador` | `Alerto2026!` |
| Citizen | `ciudadano` | `Alerto2026!` |

Cliente M2M:

```text
clientId: rules-engine
clientSecret: rules-engine-secret
```

## Roles y permisos

| Rol | Permisos principales |
|---|---|
| Admin | Gestion total, usuarios, geocercas y borrado administrativo de alertas |
| Operator | Crear, actualizar, aprobar, rechazar, cancelar y confirmar alertas |
| Analyst | Aprobar, rechazar, cancelar, difundir y consultar confirmaciones |
| Auditor | Consultar informacion operativa |
| Citizen | Consultar alertas/geocercas/clima, crear reportes y confirmar alertas activas |
| RulesEngine | Cliente M2M para lectura y difusion permitida |

## Endpoints principales

### Autenticacion

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/verify-2fa`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/m2m/token`
- `POST /api/v1/auth/2fa/setup`
- `POST /api/v1/auth/2fa/enable`

### Alertas

- `GET /api/v1/alerts`
- `GET /api/v1/alerts/{id}`
- `POST /api/v1/alerts`
- `PUT /api/v1/alerts/{id}`
- `DELETE /api/v1/alerts/{id}`
- `POST /api/v1/alerts/{id}/approve`
- `POST /api/v1/alerts/{id}/reject`
- `POST /api/v1/alerts/{id}/cancel`
- `POST /api/v1/alerts/{id}/dispatch`
- `POST /api/v1/alerts/{id}/citizen-confirm`
- `GET /api/v1/alerts/{id}/citizen-confirmations`

### Meteorologia

- `GET /api/v1/weather/dashboard?latitude={lat}&longitude={lon}`
- `GET /api/v1/weather/history?latitude={lat}&longitude={lon}&fromUtc={from}&toUtc={to}`

### Geocercas

- `GET /api/v1/geofences`
- `GET /api/v1/geofences/{id}`
- `POST /api/v1/geofences`
- `PUT /api/v1/geofences/{id}`
- `POST /api/v1/geofences/{id}/activate`
- `POST /api/v1/geofences/{id}/deactivate`

### Usuarios

- `GET /api/v1/users`
- `GET /api/v1/users/{id}`
- `POST /api/v1/users`
- `PUT /api/v1/users/{id}`
- `POST /api/v1/users/{id}/activate`
- `POST /api/v1/users/{id}/deactivate`

### Observabilidad

- `GET /metrics/basic`
- `GET /health/live`
- `GET /health/ready`

## Reglas de negocio relevantes

- Toda alerta nace en estado `Pending`.
- `Admin`, `Operator` y `Citizen` pueden crear alertas.
- Solo alertas `Pending` pueden aprobarse o rechazarse.
- La aprobacion vence a los 3 minutos desde la creacion.
- `Admin`, `Analyst` y `RulesEngine` pueden difundir alertas.
- Solo alertas `Approved` o `Broadcasted` pueden difundirse.
- Solo alertas `Approved` o `Broadcasted` pueden recibir confirmacion ciudadana.
- Cada usuario puede confirmar una misma alerta una sola vez.
- Solo `Admin` puede eliminar administrativamente una alerta.
- La eliminacion de alertas es logica: se marca `IsDeleted`, no se borra la fila.
- Usuarios y geocercas se activan o inactivan; no se eliminan fisicamente.
- Las lecturas de clima se persisten en base de datos.
- Riesgo meteorologico `High` o `Critical` puede generar una alerta automatica.
- Se usa concurrencia optimista mediante `Version`.
- Las acciones criticas generan auditoria.

## Ejemplos HTTP

### Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "AlertoAdmin123!"
}
```

### Crear alerta

```http
POST /api/v1/alerts
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Creciente subita rio Medellin",
  "description": "Se detecta aumento acelerado del caudal con riesgo para sectores riberenos.",
  "severity": "Critical",
  "sourceSystem": "Tablero COE",
  "address": "Av. Regional con Calle 30, Medellin",
  "latitude": 6.230145,
  "longitude": -75.573921,
  "geofenceId": "{geofenceId}"
}
```

### Eliminar administrativamente una alerta

```http
DELETE /api/v1/alerts/{id}
Authorization: Bearer {token-admin}
Content-Type: application/json

{
  "expectedVersion": 0,
  "reason": "Registro retirado por validacion administrativa."
}
```

### Confirmar alerta

```http
POST /api/v1/alerts/{id}/citizen-confirm
Authorization: Bearer {token}
Content-Type: application/json

{
  "notes": "La situacion fue confirmada en campo."
}
```

### Consultar clima

```http
GET /api/v1/weather/dashboard?latitude=6.244203&longitude=-75.581211
Authorization: Bearer {token}
Accept: application/json
```

## Interfaz web

La interfaz esta servida por la misma API desde `src/Alerto.Api/wwwroot`.
Permite:

- iniciar sesion;
- usar accesos demo para `admin`, `operador` y `ciudadano`;
- visualizar el logo de Alerto en login y aplicacion;
- mostrar pie institucional con imagen de Facultad de Ingenieria, docente,
  desarrolladores y GitHub;
- consultar y crear alertas;
- reportar alertas ciudadanas;
- aprobar, rechazar, cancelar, confirmar o eliminar segun el rol;
- consultar clima por coordenadas;
- visualizar ubicaciones en mapa;

## Pruebas y validacion

Ejecutar todas las pruebas:

```bash
dotnet test Alerto.sln
```

Ejecutar por proyecto:

```bash
dotnet test tests/Alerto.DomainTests/Alerto.DomainTests.csproj
dotnet test tests/Alerto.ArchitectureTests/Alerto.ArchitectureTests.csproj
dotnet test tests/Alerto.IntegrationTests/Alerto.IntegrationTests.csproj
```

La coleccion importable de Postman esta en:

```text
Coleccion de Postman.postman_collection.json
```

Ese archivo se importa directamente desde Postman e incluye login, endpoints
protegidos, CRUD de alertas, borrado logico, confirmaciones ciudadanas, clima,
usuarios, geocercas, observabilidad, refresh token y logout.

## Flujo 2FA

El segundo factor esta implementado con TOTP:

1. Iniciar sesion normalmente.
2. Ejecutar `POST /api/v1/auth/2fa/setup` con Bearer token.
3. Registrar el `secret` o `provisioningUri` en una app autenticadora.
4. Ejecutar `POST /api/v1/auth/2fa/enable` con el codigo de 6 digitos.
5. En el siguiente login, si `requiresTwoFactor` es `true`, ejecutar
   `POST /api/v1/auth/verify-2fa` con `twoFactorToken` y el codigo vigente.

## Documentacion del checkpoint

El manual tecnico esta en:

```text
CheckPoint 3. 28.04.26.md
```

Incluye contexto funcional, arquitectura, modelo de datos, contrato de API,
seguridad, pruebas, manejo de errores y conclusiones tecnicas.

## Manejo de errores

La API usa `GlobalExceptionHandlingMiddleware` y respuestas estructuradas en
`application/problem+json`.

Ejemplo:

```json
{
  "type": "about:blank",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Se requiere un Bearer token valido para acceder al recurso.",
  "instance": "/api/v1/alerts",
  "traceId": "0HN..."
}
```

Codigos usados: `200`, `201`, `204`, `400`, `401`, `403`, `404`, `409`,
`422`, `429`, `500` y `502`.

## Posibles mejoras futuras

- Ampliar evidencias de pruebas con capturas o reportes.
- Agregar mas pruebas de integracion para clima y confirmaciones ciudadanas.
- Incorporar OpenTelemetry.
- Integrar proveedores institucionales reales adicionales.
- Usar PostGIS para consultas geoespaciales mas avanzadas.
