# CheckPoint 3. 28.04.26

# Manual Tecnico - Alerto Management API

## 1. Introduccion

Alerto Management API es una API RESTful versionada desarrollada en .NET 8
para administrar alertas civiles georreferenciadas. La solucion fue disenada
como un servicio orientado a capacidades de negocio: creacion de alertas,
validacion operativa, aprobacion, difusion, consulta meteorologica,
confirmaciones ciudadanas, administracion de geocercas, gestion de usuarios,
autenticacion y auditoria.

## 2. Contexto funcional

### 2.1 Necesidad que soluciona

En escenarios de emergencia, una organizacion necesita registrar alertas,
asociarlas a zonas geograficas, validar su informacion, aprobarlas y difundirlas
sin perder trazabilidad. Alerto centraliza ese proceso mediante una API segura
que puede ser consumida por operadores humanos, ciudadanos y por otros sistemas.
Tambien permite consultar informacion meteorologica real para apoyar la
deteccion temprana de riesgos por precipitacion.

### 2.2 Usuarios que consumen la API

- Administrador: gestiona usuarios, geocercas y eliminacion administrativa.
- Operador: crea, actualiza, aprueba, rechaza, cancela y confirma alertas.
- Analista: aprueba, rechaza, cancela, difunde alertas y consulta confirmaciones.
- Auditor: consulta informacion operativa.
- Ciudadano: consulta alertas, geocercas y clima; reporta alertas pendientes y
  confirma alertas activas desde campo.
- Rules Engine: cliente maquina a maquina para integraciones.

### 2.3 Proceso de negocio soportado

1. Un administrador, operador, ciudadano o sistema autorizado registra una
   alerta.
2. La alerta queda en estado `Pending`.
3. Un administrador, operador o analista la aprueba o rechaza dentro de la
   ventana definida.
4. Una alerta aprobada puede difundirse por canales externos por roles
   autorizados.
5. Un ciudadano u operador puede confirmar que la alerta corresponde a una
   situacion real.
6. El modulo meteorologico consulta Open-Meteo, guarda lecturas y puede crear
   alertas automaticas cuando el riesgo es alto o critico.
7. Todas las acciones criticas quedan auditadas.
8. Las alertas no se eliminan fisicamente; si un administrador las retira,
   quedan marcadas con borrado logico.

## 3. Arquitectura de la solucion

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

### 3.2 Separacion de responsabilidades

- `Alerto.Api`: expone el contrato HTTP, versionamiento, autenticacion,
  autorizacion, Swagger, archivos estaticos de la interfaz y manejo global de
  errores.
- `Alerto.Application`: implementa casos de uso, validaciones, DTOs y puertos
  para persistencia, cache, JWT e integraciones.
- `Alerto.Domain`: concentra reglas de negocio e invariantes de alertas,
  usuarios, geocercas, lecturas climaticas, confirmaciones ciudadanas y tokens.
- `Alerto.Infrastructure`: implementa repositorios, EF Core, PostgreSQL,
  Redis, generacion JWT, TOTP, cliente Open-Meteo y outbox.

### 3.3 Principios SOA aplicados

- Desacoplamiento: los casos de uso dependen de interfaces, no de detalles de
  infraestructura.
- Contrato explicito: la API expone rutas versionadas y documentacion Swagger.
- Stateless: cada request se autentica con JWT; el servidor no depende de
  sesion en memoria.
- Reutilizacion: el mismo servicio atiende frontend, Postman y clientes M2M.
- Interoperabilidad: HTTP, JSON, JWT Bearer y ProblemDetails.
- Gobernanza: versionamiento, auditoria, health checks, logs y rate limiting.

### 3.4 Interfaz de captura

La interfaz basica se sirve desde `src/Alerto.Api/wwwroot` por la misma API. Es
una aplicacion HTML, CSS y JavaScript conectada directamente a los endpoints
versionados. Incluye:

- login con usuarios demo `admin`, `operador` y `ciudadano`;
- logo de Alerto en el login y en la aplicacion;
- pie institucional con imagen de Facultad de Ingenieria, docente,
  desarrolladores y enlace GitHub;
- paneles por rol para alertas, reportes ciudadanos, geocercas, usuarios, clima
  y confirmaciones;
- mapas con Leaflet y consulta meteorologica con Open-Meteo.

## 4. Modelo de datos

### 4.1 Diagrama Entidad-Relacion

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

| Tabla | Proposito |
|---|---|
| `alerts` | Registro principal de alertas civiles. |
| `alert_dispatches` | Evidencia de difusion por canal o proveedor. |
| `alert_citizen_confirmations` | Confirmaciones ciudadanas asociadas a una alerta. |
| `geofences` | Zonas geograficas operativas. |
| `weather_readings` | Lecturas meteorologicas consultadas y persistidas. |
| `users` | Usuarios humanos con roles y credenciales. |
| `refresh_tokens` | Tokens persistidos para renovacion de sesiones. |
| `audit_trails` | Auditoria de acciones criticas. |
| `outbox_messages` | Mensajes pendientes para integraciones/eventos. |

Campos criticos:

| Campo | Tabla | Descripcion |
|---|---|---|
| `Version` | varias | Control de concurrencia optimista. |
| `Status` | `alerts` | Estado de negocio: Pending, Approved, Rejected, Broadcasted, Cancelled. |
| `IsDeleted` | `alerts` | Borrado logico administrativo; no elimina fisicamente. |
| `DeletionReason` | `alerts` | Justificacion del retiro administrativo. |
| `RiskLevel` | `weather_readings` | Nivel calculado de riesgo por precipitacion: Low, Moderate, High, Critical. |
| `AutoAlertId` | `weather_readings` | Alerta creada automaticamente cuando el riesgo amerita accion. |
| `ConfirmedByUserId` | `alert_citizen_confirmations` | Usuario que confirma la existencia de la situacion en campo. |
| `Role` | `users` | Base de autorizacion por politicas. |
| `DetailsJson` | `audit_trails` | Detalle estructurado de la accion auditada. |

## 5. Diseno del servicio

Todas las rutas funcionales principales usan versionamiento `/api/v1/`.

### 5.1 Autenticacion

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

Codigos: `200`, `400`, `401`, `429`.

#### POST `/api/v1/auth/m2m/token`

Body:

```json
{
  "clientId": "rules-engine",
  "clientSecret": "rules-engine-secret"
}
```

Codigos: `200`, `400`, `401`, `429`.

### 5.2 Alertas

#### GET `/api/v1/alerts`

Headers:

```http
Authorization: Bearer {token}
```

Query opcional: `status`, `geofenceId`, `severity`, `createdFromUtc`,
`createdToUtc`, `pageNumber`, `pageSize`.

Codigos: `200`, `400`, `401`, `403`.

#### GET `/api/v1/alerts/{id}`

Consulta una alerta por identificador.

Codigos: `200`, `401`, `403`, `404`.

#### POST `/api/v1/alerts`

Crea una alerta en estado `Pending`. Puede ser usada por `Admin`, `Operator` y
`Citizen`; en la interfaz ciudadana funciona como reporte pendiente de revision.

Body:

```json
{
  "title": "Creciente subita rio Medellin",
  "description": "Se detecta aumento acelerado del caudal.",
  "severity": "Critical",
  "sourceSystem": "Tablero COE",
  "address": "Av. Regional con Calle 30, Medellin",
  "latitude": 6.230145,
  "longitude": -75.573921,
  "geofenceId": "11111111-1111-1111-1111-111111111111"
}
```

Codigos: `201`, `400`, `401`, `403`, `404`.

#### PUT `/api/v1/alerts/{id}`

Actualiza una alerta pendiente usando concurrencia optimista.

Codigos: `200`, `400`, `401`, `403`, `404`, `409`.

#### DELETE `/api/v1/alerts/{id}`

Elimina administrativamente una alerta. Solo puede ejecutarlo un usuario con
rol `Admin`. La API no hace borrado fisico: marca `IsDeleted = true`, conserva
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
  "reason": "Registro retirado por validacion administrativa."
}
```

Respuesta esperada: `204 No Content`.

Codigos: `204`, `400`, `401`, `403`, `404`, `409`, `422`.

#### POST `/api/v1/alerts/{id}/approve`

Aprueba una alerta pendiente. Disponible para `Admin`, `Operator` y `Analyst`.

Body:

```json
{
  "expectedVersion": 0
}
```

Codigos: `200`, `400`, `401`, `403`, `404`, `409`, `422`.

#### POST `/api/v1/alerts/{id}/reject`

Rechaza una alerta pendiente. Disponible para `Admin`, `Operator` y `Analyst`.

Body:

```json
{
  "expectedVersion": 0,
  "reason": "Informacion insuficiente."
}
```

Codigos: `200`, `400`, `401`, `403`, `404`, `409`, `422`.

#### POST `/api/v1/alerts/{id}/dispatch`

Registra la difusion de una alerta aprobada. Disponible para `Admin`, `Analyst`
y `RulesEngine`.

Codigos: `200`, `400`, `401`, `403`, `404`, `409`, `422`.

#### POST `/api/v1/alerts/{id}/citizen-confirm`

Registra que un ciudadano u operador confirma una alerta aprobada o difundida.
Solo se permite una confirmacion por usuario y alerta.

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

Codigos: `201`, `400`, `401`, `403`, `404`, `409`, `422`.

#### GET `/api/v1/alerts/{id}/citizen-confirmations`

Consulta las confirmaciones ciudadanas registradas para una alerta. Disponible
para `Admin`, `Operator` y `Analyst`.

Codigos: `200`, `401`, `403`, `404`.

### 5.3 Meteorologia

#### GET `/api/v1/weather/dashboard`

Consulta Open-Meteo para las coordenadas enviadas, persiste la lectura en base
de datos y devuelve el resumen de riesgo. Si el riesgo calculado es `High` o
`Critical`, el servicio puede crear una alerta automatica asociada a la lectura.

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

Codigos: `200`, `400`, `401`, `403`, `502`.

#### GET `/api/v1/weather/history`

Consulta lecturas meteorologicas persistidas para unas coordenadas y un rango
UTC. Si no se envian fechas, usa las ultimas 24 horas.

Query opcional:

```text
latitude=6.244203&longitude=-75.581211&fromUtc=2026-05-12T00:00:00Z&toUtc=2026-05-13T00:00:00Z
```

Codigos: `200`, `400`, `401`, `403`.

### 5.4 Geocercas

Endpoints principales:

| Metodo | URL | Proposito |
|---|---|---|
| GET | `/api/v1/geofences` | Listar geocercas. |
| GET | `/api/v1/geofences/{id}` | Consultar detalle. |
| POST | `/api/v1/geofences` | Crear geocerca. |
| PUT | `/api/v1/geofences/{id}` | Actualizar geocerca. |
| POST | `/api/v1/geofences/{id}/activate` | Activar. |
| POST | `/api/v1/geofences/{id}/deactivate` | Inactivar. |

### 5.5 Usuarios

Endpoints principales:

| Metodo | URL | Proposito |
|---|---|---|
| GET | `/api/v1/users` | Listar usuarios. |
| GET | `/api/v1/users/{id}` | Consultar detalle. |
| POST | `/api/v1/users` | Crear usuario. |
| PUT | `/api/v1/users/{id}` | Actualizar usuario. |
| POST | `/api/v1/users/{id}/activate` | Activar usuario. |
| POST | `/api/v1/users/{id}/deactivate` | Inactivar usuario. |

## 6. Seguridad

### 6.1 Flujo JWT

1. El cliente envia credenciales a `/api/v1/auth/login`.
2. La API valida usuario, password y estado.
3. Si el usuario no requiere 2FA, emite `accessToken` y `refreshToken`.
4. El cliente envia el token en `Authorization: Bearer {token}`.
5. Los endpoints protegidos validan firma, issuer, audience, expiracion y rol.

### 6.2 Endpoints protegidos

- Alertas: lectura por `Admin`, `Operator`, `Analyst`, `Auditor`,
  `RulesEngine` y `Citizen`.
- Creacion de alertas: `Admin`, `Operator` y `Citizen`.
- Actualizacion de alertas: `Admin` y `Operator`.
- Aprobacion, rechazo y cancelacion: `Admin`, `Operator` y `Analyst`.
- Difusion: `Admin`, `Analyst` y `RulesEngine`.
- Eliminacion administrativa de alertas: solo `Admin`.
- Confirmaciones ciudadanas: registro por `Admin`, `Operator` y `Citizen`;
  lectura de confirmaciones por `Admin`, `Operator` y `Analyst`.
- Meteorologia: consulta protegida por los mismos roles con permiso de lectura
  de alertas.
- Geocercas: administracion solo `Admin`.
- Usuarios: administracion solo `Admin`.

### 6.3 Header obligatorio

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI...
```

### 6.4 Usuarios demo

El inicializador de base de datos crea o verifica usuarios demo para facilitar
la validacion funcional desde interfaz y Postman:

| Rol | Usuario | Password |
|---|---|---|
| Admin | `admin` | `AlertoAdmin123!` |
| Operator | `operador` | `Alerto2026!` |
| Citizen | `ciudadano` | `Alerto2026!` |

## 7. Implementacion tecnica

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

### 7.2 Tecnologias utilizadas

| Tecnologia | Justificacion |
|---|---|
| .NET 8 | Plataforma LTS para APIs HTTP. |
| ASP.NET Core | Routing, controllers, DI, middlewares y seguridad. |
| EF Core | ORM y migraciones para PostgreSQL. |
| PostgreSQL | Persistencia relacional robusta. |
| Redis | Cache, idempotencia y soporte de procesos distribuidos. |
| JWT Bearer | Autenticacion stateless. |
| TOTP | Segundo factor con codigos temporales para usuarios que lo habiliten. |
| FluentValidation | Validacion declarativa de requests. |
| Swagger/OpenAPI | Documentacion interactiva del contrato. |
| xUnit | Pruebas unitarias, de arquitectura e integracion. |
| Open-Meteo | Fuente externa de datos meteorologicos para riesgo por precipitacion. |
| HTML, CSS y JavaScript | Interfaz basica conectada directamente a la API. |
| Leaflet | Visualizacion de coordenadas y contexto geografico en la interfaz. |
| Assets institucionales | Logo Alerto e imagen de Facultad de Ingenieria en login y pie de pagina. |

## 8. Pruebas

### 8.1 Evidencia Postman

El archivo `Coleccion de Postman.postman_collection.json` contiene la coleccion
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
- Consultar dashboard meteorologico.
- Consultar historial meteorologico.
- Eliminar administrativamente alerta.
- Refresh token.
- Logout.

### 8.2 Casos de prueba esperados

| Caso | Resultado esperado |
|---|---|
| Login valido | `200 OK` con JWT. |
| GET protegido sin token | `401 Unauthorized`. |
| Crear alerta con token Admin/Operator/Citizen | `201 Created`. |
| Actualizar con version incorrecta | `409 Conflict`. |
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
| Consultar dashboard meteorologico | `200 OK` y lectura persistida. |
| Consultar historial con rango invalido | `400 Bad Request`. |

## 9. Manejo de errores

La API usa `GlobalExceptionHandlingMiddleware` y respuestas tipo
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

Codigos contemplados:

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

Swagger esta configurado en ambiente `Development`. Al ejecutar la API puede
consultarse en:

```text
http://localhost:5070/swagger
```

## 11. Conclusiones tecnicas

Alerto cumple con el enfoque de servicios porque expone capacidades funcionales
de negocio mediante contratos HTTP versionados, seguridad JWT, persistencia real
y separacion clara de responsabilidades. El borrado de alertas se implementa
como eliminacion administrativa logica para proteger la trazabilidad de eventos
generados por otros sistemas, manteniendo cumplimiento CRUD sin destruir datos
historicos. La ampliacion meteorologica y las confirmaciones ciudadanas
fortalecen el contexto funcional porque conectan la API con datos externos y
validacion en campo. La interfaz web y los usuarios demo permiten evidenciar
la conexion cliente-servidor sin depender unicamente de Postman.
