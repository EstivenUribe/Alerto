# CheckPoint 3. 28.04.26

# Manual Técnico - Alerto Management API

## 1. Introducción

Alerto Management API es una API RESTful versionada desarrollada en .NET 8
para administrar alertas civiles georreferenciadas. La solución fue diseñada
como un servicio orientado a capacidades de negocio: creación de alertas,
validación operativa, aprobación, difusión, consulta meteorológica,
confirmaciones ciudadanas, administración de geocercas, gestión de usuarios,
autenticación y auditoría.

## 2. Contexto funcional

### 2.1 Necesidad que soluciona

En escenarios de emergencia, una organización necesita registrar alertas,
asociarlas a zonas geográficas, validar su información, aprobarlas y difundirlas
sin perder trazabilidad. Alerto centraliza ese proceso mediante una API segura
que puede ser consumida por operadores humanos, ciudadanos y por otros sistemas.
También permite consultar información meteorológica real para apoyar la
detección temprana de riesgos por precipitación.

### 2.2 Usuarios que consumen la API

- Administrador: gestiona usuarios, geocercas y eliminación administrativa.
- Operador: crea, actualiza, aprueba, rechaza, cancela y confirma alertas.
- Analista: aprueba, rechaza, cancela, difunde alertas y consulta confirmaciones.
- Auditor: consulta información operativa.
- Ciudadano: consulta alertas, geocercas y clima; reporta alertas pendientes y
  confirma alertas activas desde campo.
- Rules Engine: cliente máquina a máquina para integraciones.

### 2.3 Proceso de negocio soportado

1. Un administrador, operador, ciudadano o sistema autorizado registra una
   alerta.
2. La alerta queda en estado `Pending`.
3. Un administrador, operador o analista la aprueba o rechaza dentro de la
   ventana definida.
4. Una alerta aprobada puede difundirse por canales externos por roles
   autorizados.
5. Un ciudadano u operador puede confirmar que la alerta corresponde a una
   situación real.
6. El módulo meteorológico consulta Open-Meteo, guarda lecturas y puede crear
   alertas automáticas cuando el riesgo es alto o crítico.
7. Todas las acciones críticas quedan auditadas.
8. Las alertas no se eliminan físicamente; si un administrador las retira,
   quedan marcadas con borrado lógico.

## 3. Arquitectura de la solución

### 3.1 Diagrama en capas

```mermaid
flowchart TB
    Cliente["Cliente / Interfaz / Postman"]
    Api["Alerto.Api\nControllers, JWT, Swagger, Middlewares"]
    App["Alerto.Application\nCasos de uso, DTOs, Validadores, Puertos"]
    Domain["Alerto.Domain\nEntidades, Value Objects, Reglas"]
    Infra["Alerto.Infrastructure\nEF Core, PostgreSQL, Redis, JWT, Repositorios"]
    Db[("PostgreSQL\nschema alerto")]
    Cache[("Redis")]
    OpenMeteo["Open-Meteo\nAPI externa"]

    Cliente -->|"HTTPS + JSON + Bearer JWT"| Api
    Api --> App
    App --> Domain
    Infra --> App
    Infra --> Domain
    Api --> Infra
    Infra --> Db
    Infra --> Cache
    Infra --> OpenMeteo
```

### 3.2 Separación de responsabilidades

- `Alerto.Api`: expone el contrato HTTP, versionamiento, autenticación,
  autorización, Swagger, archivos estáticos de la interfaz y manejo global de
  errores.
- `Alerto.Application`: implementa casos de uso, validaciones, DTOs y puertos
  para persistencia, cache, JWT e integraciones.
- `Alerto.Domain`: concentra reglas de negocio e invariantes de alertas,
  usuarios, geocercas, lecturas climáticas, confirmaciones ciudadanas y tokens.
- `Alerto.Infrastructure`: implementa repositorios, EF Core, PostgreSQL,
  Redis, generación JWT, TOTP, cliente Open-Meteo y outbox.

### 3.3 Principios SOA aplicados

- Desacoplamiento: los casos de uso dependen de interfaces, no de detalles de
  infraestructura.
- Contrato explícito: la API expone rutas versionadas y documentación Swagger.
- Stateless: cada request se autentica con JWT; el servidor no depende de
  sesión en memoria.
- Reutilización: el mismo servicio atiende frontend, Postman y clientes M2M.
- Interoperabilidad: HTTP, JSON, JWT Bearer y ProblemDetails.
- Gobernanza: versionamiento, auditoría, health checks, logs y rate limiting.

### 3.4 Interfaz de captura

La interfaz básica se sirve desde `src/Alerto.Api/wwwroot` por la misma API. Es
una aplicación HTML, CSS y JavaScript conectada directamente a los endpoints
versionados. Incluye:

- login con usuarios demo `admin`, `operador` y `ciudadano`;
- logo de Alerto en el login y en la aplicación;
- pie institucional con imagen de Facultad de Ingeniería, docente,
  desarrolladores y enlace GitHub;
- paneles por rol para alertas, reportes ciudadanos, geocercas, usuarios, clima
  y confirmaciones;
- mapas con Leaflet y consulta meteorológica con Open-Meteo.

## 4. Modelo de datos

### 4.1 Diagrama Entidad-Relación

```mermaid
erDiagram
    USERS ||--o{ REFRESH_TOKENS : owns
    ALERTS ||--o{ ALERT_DISPATCHES : has
    ALERTS ||--o{ APPROVAL_RECORDS : has
    ALERTS ||--o{ ALERT_CITIZEN_CONFIRMATIONS : receives
    ALERTS }o--|| GEOFENCES : belongs_to
    WEATHER_READINGS }o--o| ALERTS : can_create
    AUDIT_TRAILS }o--|| USERS : actor

    USERS {
        uuid Id
        string Username
        string DisplayName
        string Email
        string Role
        string PasswordHash
        bool IsActive
        bool IsTwoFactorEnabled
        string TotpSecret
        int Version
    }

    ALERTS {
        uuid Id
        string Title
        string Description
        string Severity
        string Status
        string SourceSystem
        string Address
        decimal Latitude
        decimal Longitude
        uuid GeofenceId
        bool IsDeleted
        uuid DeletedByUserId
        datetime DeletedAtUtc
        string DeletionReason
        int Version
    }

    GEOFENCES {
        uuid Id
        string Code
        string Name
        string PolygonWkt
        string Neighborhood
        bool IsActive
        int Version
    }

    ALERT_DISPATCHES {
        uuid Id
        uuid AlertId
        string Channel
        string Destination
        string ProviderReference
        uuid DispatchedByUserId
    }

    ALERT_CITIZEN_CONFIRMATIONS {
        uuid Id
        uuid AlertId
        uuid ConfirmedByUserId
        string Notes
        datetime CreatedAtUtc
    }

    WEATHER_READINGS {
        uuid Id
        decimal Latitude
        decimal Longitude
        decimal PrecipitationMmPerHour
        int PrecipitationProbabilityPercent
        int WeatherCode
        string RiskLevel
        uuid AutoAlertId
        datetime RecordedAtUtc
    }

    REFRESH_TOKENS {
        uuid Id
        uuid UserId
        string Token
        datetime ExpiresAtUtc
        datetime RevokedAtUtc
        string CreatedByIp
    }

    AUDIT_TRAILS {
        uuid Id
        uuid ActorId
        string Action
        string EntityName
        uuid EntityId
        json DetailsJson
        string TraceId
    }
```

### 4.2 Diccionario de datos

| Tabla | Propósito |
|---|---|
| `alerts` | Registro principal de alertas civiles. |
| `alert_dispatches` | Evidencia de difusión por canal o proveedor. |
| `alert_citizen_confirmations` | Confirmaciones ciudadanas asociadas a una alerta. |
| `geofences` | Zonas geográficas operativas. |
| `weather_readings` | Lecturas meteorológicas consultadas y persistidas. |
| `users` | Usuarios humanos con roles y credenciales. |
| `refresh_tokens` | Tokens persistidos para renovación de sesiones. |
| `audit_trails` | Auditoría de acciones críticas. |
| `outbox_messages` | Mensajes pendientes para integraciones/eventos. |

Campos críticos:

| Campo | Tabla | Descripción |
|---|---|---|
| `Version` | varias | Control de concurrencia optimista. |
| `Status` | `alerts` | Estado de negocio: Pending, Approved, Rejected, Broadcasted, Cancelled. |
| `IsDeleted` | `alerts` | Borrado lógico administrativo; no elimina físicamente. |
| `DeletionReason` | `alerts` | Justificación del retiro administrativo. |
| `RiskLevel` | `weather_readings` | Nivel calculado de riesgo por precipitación: Low, Moderate, High, Critical. |
| `AutoAlertId` | `weather_readings` | Alerta creada automáticamente cuando el riesgo amerita acción. |
| `ConfirmedByUserId` | `alert_citizen_confirmations` | Usuario que confirma la existencia de la situación en campo. |
| `Role` | `users` | Base de autorización por políticas. |
| `DetailsJson` | `audit_trails` | Detalle estructurado de la acción auditada. |

## 5. Diseño del servicio

Todas las rutas funcionales principales usan versionamiento `/api/v1/`.

### 5.1 Autenticación

#### POST `/api/v1/auth/login`

Headers:

```http
Content-Type: application/json
```

Body:

```json
{
  "username": "admin",
  "password": "AlertoAdmin123!"
}
```

Respuesta `200 OK`:

```json
{
  "tokenType": "Bearer",
  "username": "admin",
  "role": "Admin",
  "requiresTwoFactor": false,
  "accessToken": "eyJ...",
  "refreshToken": "..."
}
```

Códigos: `200`, `400`, `401`, `429`.

#### POST `/api/v1/auth/m2m/token`

Body:

```json
{
  "clientId": "rules-engine",
  "clientSecret": "rules-engine-secret"
}
```

Códigos: `200`, `400`, `401`, `429`.

### 5.2 Alertas

#### GET `/api/v1/alerts`

Headers:

```http
Authorization: Bearer {token}
```

Query opcional: `status`, `geofenceId`, `severity`, `createdFromUtc`,
`createdToUtc`, `pageNumber`, `pageSize`.

Códigos: `200`, `400`, `401`, `403`.

#### GET `/api/v1/alerts/{id}`

Consulta una alerta por identificador.

Códigos: `200`, `401`, `403`, `404`.

#### POST `/api/v1/alerts`

Crea una alerta en estado `Pending`. Puede ser usada por `Admin`, `Operator` y
`Citizen`; en la interfaz ciudadana funciona como reporte pendiente de revisión.

Body:

```json
{
  "title": "Creciente súbita río Medellín",
  "description": "Se detecta aumento acelerado del caudal.",
  "severity": "Critical",
  "sourceSystem": "Tablero COE",
  "address": "Av. Regional con Calle 30, Medellín",
  "latitude": 6.230145,
  "longitude": -75.573921,
  "geofenceId": "11111111-1111-1111-1111-111111111111"
}
```

Códigos: `201`, `400`, `401`, `403`, `404`.

#### PUT `/api/v1/alerts/{id}`

Actualiza una alerta pendiente usando concurrencia optimista.

Códigos: `200`, `400`, `401`, `403`, `404`, `409`.

#### DELETE `/api/v1/alerts/{id}`

Elimina administrativamente una alerta. Solo puede ejecutarlo un usuario con
rol `Admin`. La API no hace borrado físico: marca `IsDeleted = true`, conserva
la fila y la oculta de consultas normales.

Headers:

```http
Authorization: Bearer {token}
Content-Type: application/json
```

Body:

```json
{
  "expectedVersion": 0,
  "reason": "Registro retirado por validación administrativa."
}
```

Respuesta esperada: `204 No Content`.

Códigos: `204`, `400`, `401`, `403`, `404`, `409`, `422`.

#### POST `/api/v1/alerts/{id}/approve`

Aprueba una alerta pendiente. Disponible para `Admin`, `Operator` y `Analyst`.

Body:

```json
{
  "expectedVersion": 0
}
```

Códigos: `200`, `400`, `401`, `403`, `404`, `409`, `422`.

#### POST `/api/v1/alerts/{id}/reject`

Rechaza una alerta pendiente. Disponible para `Admin`, `Operator` y `Analyst`.

Body:

```json
{
  "expectedVersion": 0,
  "reason": "Información insuficiente."
}
```

Códigos: `200`, `400`, `401`, `403`, `404`, `409`, `422`.

#### POST `/api/v1/alerts/{id}/dispatch`

Registra la difusión de una alerta aprobada. Disponible para `Admin`, `Analyst`
y `RulesEngine`.

Códigos: `200`, `400`, `401`, `403`, `404`, `409`, `422`.

#### POST `/api/v1/alerts/{id}/citizen-confirm`

Registra que un ciudadano u operador confirma una alerta aprobada o difundida.
Solo se permite una confirmación por usuario y alerta.

Headers:

```http
Authorization: Bearer {token}
Content-Type: application/json
```

Body:

```json
{
  "notes": "La creciente es visible desde el puente de la zona."
}
```

Respuesta `201 Created`:

```json
{
  "id": "33333333-3333-3333-3333-333333333333",
  "alertId": "11111111-1111-1111-1111-111111111111",
  "confirmedByUserId": "22222222-2222-2222-2222-222222222222",
  "notes": "La creciente es visible desde el puente de la zona.",
  "confirmedAtUtc": "2026-05-12T22:40:00Z"
}
```

Códigos: `201`, `400`, `401`, `403`, `404`, `409`, `422`.

#### GET `/api/v1/alerts/{id}/citizen-confirmations`

Consulta las confirmaciones ciudadanas registradas para una alerta. Disponible
para `Admin`, `Operator` y `Analyst`.

Códigos: `200`, `401`, `403`, `404`.

### 5.3 Meteorología

#### GET `/api/v1/weather/dashboard`

Consulta Open-Meteo para las coordenadas enviadas, persiste la lectura en base
de datos y devuelve el resumen de riesgo. Si el riesgo calculado es `High` o
`Critical`, el servicio puede crear una alerta automática asociada a la lectura.

Headers:

```http
Authorization: Bearer {token}
```

Query:

```text
latitude=6.244203&longitude=-75.581211
```

Respuesta `200 OK`:

```json
{
  "latitude": 6.244203,
  "longitude": -75.581211,
  "precipitationMmPerHour": 8.2,
  "precipitationProbabilityPercent": 72,
  "weatherCode": 61,
  "weatherDescription": "Lluvia",
  "riskLevel": "High",
  "autoAlertCreated": true,
  "autoAlertId": "44444444-4444-4444-4444-444444444444",
  "recordedAtUtc": "2026-05-12T22:45:00Z",
  "isFromCache": false,
  "hourlyForecast": []
}
```

Códigos: `200`, `400`, `401`, `403`, `502`.

#### GET `/api/v1/weather/history`

Consulta lecturas meteorológicas persistidas para unas coordenadas y un rango
UTC. Si no se envían fechas, usa las últimas 24 horas.

Query opcional:

```text
latitude=6.244203&longitude=-75.581211&fromUtc=2026-05-12T00:00:00Z&toUtc=2026-05-13T00:00:00Z
```

Códigos: `200`, `400`, `401`, `403`.

### 5.4 Geocercas

Endpoints principales:

| Método | URL | Propósito |
|---|---|---|
| GET | `/api/v1/geofences` | Listar geocercas. |
| GET | `/api/v1/geofences/{id}` | Consultar detalle. |
| POST | `/api/v1/geofences` | Crear geocerca. |
| PUT | `/api/v1/geofences/{id}` | Actualizar geocerca. |
| POST | `/api/v1/geofences/{id}/activate` | Activar. |
| POST | `/api/v1/geofences/{id}/deactivate` | Inactivar. |

### 5.5 Usuarios

Endpoints principales:

| Método | URL | Propósito |
|---|---|---|
| GET | `/api/v1/users` | Listar usuarios. |
| GET | `/api/v1/users/{id}` | Consultar detalle. |
| POST | `/api/v1/users` | Crear usuario. |
| PUT | `/api/v1/users/{id}` | Actualizar usuario. |
| POST | `/api/v1/users/{id}/activate` | Activar usuario. |
| POST | `/api/v1/users/{id}/deactivate` | Inactivar usuario. |

## 6. Seguridad

### 6.1 Flujo JWT

1. El cliente envía credenciales a `/api/v1/auth/login`.
2. La API valida usuario, password y estado.
3. Si el usuario no requiere 2FA, emite `accessToken` y `refreshToken`.
4. El cliente envía el token en `Authorization: Bearer {token}`.
5. Los endpoints protegidos validan firma, issuer, audience, expiración y rol.

### 6.2 Endpoints protegidos

- Alertas: lectura por `Admin`, `Operator`, `Analyst`, `Auditor`,
  `RulesEngine` y `Citizen`.
- Creación de alertas: `Admin`, `Operator` y `Citizen`.
- Actualización de alertas: `Admin` y `Operator`.
- Aprobación, rechazo y cancelación: `Admin`, `Operator` y `Analyst`.
- Difusión: `Admin`, `Analyst` y `RulesEngine`.
- Eliminación administrativa de alertas: solo `Admin`.
- Confirmaciones ciudadanas: registro por `Admin`, `Operator` y `Citizen`;
  lectura de confirmaciones por `Admin`, `Operator` y `Analyst`.
- Meteorología: consulta protegida por los mismos roles con permiso de lectura
  de alertas.
- Geocercas: administración solo `Admin`.
- Usuarios: administración solo `Admin`.

### 6.3 Header obligatorio

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI...
```

### 6.4 Usuarios demo

El inicializador de base de datos crea o verifica usuarios demo para facilitar
la validación funcional desde interfaz y Postman:

| Rol | Usuario | Password |
|---|---|---|
| Admin | `admin` | `AlertoAdmin123!` |
| Operator | `operador` | `Alerto2026!` |
| Citizen | `ciudadano` | `Alerto2026!` |

## 7. Implementación técnica

### 7.1 Estructura del proyecto

```text
Alerto.sln
src/
  Alerto.Api/
  Alerto.Application/
  Alerto.Domain/
  Alerto.Infrastructure/
tests/
  Alerto.DomainTests/
  Alerto.ArchitectureTests/
  Alerto.IntegrationTests/
docker-compose.yml
```

### 7.2 Tecnologías utilizadas

| Tecnología | Justificación |
|---|---|
| .NET 8 | Plataforma LTS para APIs HTTP. |
| ASP.NET Core | Routing, controllers, DI, middlewares y seguridad. |
| EF Core | ORM y migraciones para PostgreSQL. |
| PostgreSQL | Persistencia relacional robusta. |
| Redis | Cache, idempotencia y soporte de procesos distribuidos. |
| JWT Bearer | Autenticación stateless. |
| TOTP | Segundo factor con códigos temporales para usuarios que lo habiliten. |
| FluentValidation | Validación declarativa de requests. |
| Swagger/OpenAPI | Documentación interactiva del contrato. |
| xUnit | Pruebas unitarias, de arquitectura e integración. |
| Open-Meteo | Fuente externa de datos meteorológicos para riesgo por precipitación. |
| HTML, CSS y JavaScript | Interfaz básica conectada directamente a la API. |
| Leaflet | Visualización de coordenadas y contexto geográfico en la interfaz. |
| Assets institucionales | Logo Alerto e imagen de Facultad de Ingeniería en login y pie de página. |

## 8. Pruebas

### 8.1 Evidencia Postman

El archivo `Coleccion de Postman.postman_collection.json` contiene la colección
importable para probar:

- Login.
- Endpoint protegido sin token.
- Consulta con token.
- Crear alerta.
- Crear alerta como ciudadano.
- Consultar alerta.
- Actualizar alerta.
- Aprobar alerta.
- Difundir alerta.
- Confirmar alerta como ciudadano u operador.
- Consultar confirmaciones ciudadanas.
- Consultar dashboard meteorológico.
- Consultar historial meteorológico.
- Eliminar administrativamente alerta.
- Refresh token.
- Logout.

### 8.2 Casos de prueba esperados

| Caso | Resultado esperado |
|---|---|
| Login válido | `200 OK` con JWT. |
| GET protegido sin token | `401 Unauthorized`. |
| Crear alerta con token Admin/Operator/Citizen | `201 Created`. |
| Actualizar con versión incorrecta | `409 Conflict`. |
| Aprobar alerta con Operator | `200 OK`. |
| Difundir alerta con Operator | `403 Forbidden`. |
| Difundir alerta con Analyst o RulesEngine | `200 OK`. |
| Eliminar alerta con usuario no Admin | `403 Forbidden`. |
| Eliminar alerta con Admin | `204 No Content`. |
| Consultar alerta eliminada | `404 Not Found`. |
| Confirmar alerta aprobada o difundida | `201 Created`. |
| Confirmar dos veces con el mismo usuario | `409 Conflict`. |
| Confirmar con notas mayores a 500 caracteres | `400 Bad Request`. |
| Consultar confirmaciones con Analyst | `200 OK`. |
| Consultar dashboard meteorológico | `200 OK` y lectura persistida. |
| Consultar historial con rango inválido | `400 Bad Request`. |

## 9. Manejo de errores

La API usa `GlobalExceptionHandlingMiddleware` y respuestas tipo
`application/problem+json`.

Ejemplo:

```json
{
  "type": "about:blank",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Se requiere un Bearer token válido para acceder al recurso.",
  "instance": "/api/v1/alerts",
  "traceId": "0HN..."
}
```

Códigos contemplados:

- `200 OK`
- `201 Created`
- `204 No Content`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `429 Too Many Requests`
- `500 Internal Server Error`
- `502 Bad Gateway`

## 10. Swagger / OpenAPI

Swagger está configurado en ambiente `Development`. Al ejecutar la API puede
consultarse en:

```text
http://localhost:5070/swagger
```

## 11. Conclusiones técnicas

Alerto cumple con el enfoque de servicios porque expone capacidades funcionales
de negocio mediante contratos HTTP versionados, seguridad JWT, persistencia real
y separación clara de responsabilidades. El borrado de alertas se implementa
como eliminación administrativa lógica para proteger la trazabilidad de eventos
generados por otros sistemas, manteniendo cumplimiento CRUD sin destruir datos
históricos. La ampliación meteorológica y las confirmaciones ciudadanas
fortalecen el contexto funcional porque conectan la API con datos externos y
validación en campo. La interfaz web y los usuarios demo permiten evidenciar
la conexión cliente-servidor sin depender únicamente de Postman.
