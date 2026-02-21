# PedidosAPI

> **Prueba Técnica Práctica** — Sistema transaccional de registro de pedidos  
> Desarrollado con **.NET 9 · Minimal API · Clean Architecture · Entity Framework Core · Polly · Scalar**

---

## 📋 Tabla de Contenidos

1. [Descripción General](#-descripción-general)
2. [Arquitectura](#-arquitectura)
3. [Estructura del Proyecto](#-estructura-del-proyecto)
4. [Proyectos en Detalle](#-proyectos-en-detalle)
   - [Domain](#pedidosapidomin)
   - [Application](#pedidosapiapplication)
   - [Infrastructure](#pedidosapiinfrastructure)
   - [API](#pedidosapiapi)
5. [Patrones y Decisiones de Diseño](#-patrones-y-decisiones-de-diseño)
6. [Circuit Breaker con Polly](#-circuit-breaker-con-polly)
7. [Manejo Transaccional](#-manejo-transaccional)
8. [Registro de Eventos](#-registro-de-eventos-logging)
9. [Manejo de Errores](#-manejo-de-errores)
10. [Base de Datos](#-base-de-datos)
11. [Endpoints](#-endpoints)
12. [Configuración y Ejecución](#-configuración-y-ejecución)
13. [Criterios de Evaluación](#-criterios-de-evaluación)
14. [Historial Git](#-historial-git)

---

## 📌 Descripción General

PedidosAPI es un sistema backend desarrollado en **.NET 9** que expone una **API REST usando Minimal API** para registrar y consultar pedidos empresariales. Cada pedido:

- Se registra en base de datos SQL Server con su detalle de productos
- Valida al cliente mediante un servicio HTTP externo simulado
- Registra eventos de auditoría en base de datos
- Mantiene consistencia total mediante transacciones SQL
- Protege la comunicación externa con Circuit Breaker + Retry + Timeout (Polly)

---

## 🏛️ Arquitectura

El proyecto implementa **Clean Architecture** (también conocida como Arquitectura Hexagonal o por capas concéntricas), donde las dependencias apuntan siempre hacia el centro:

```
┌────────────────────────────────────────────────────────────────────────┐
│                         PedidosAPI.API                                 │
│     Minimal API · Scalar · ExceptionMiddleware · Program.cs            │
│                                                                        │
│  Endpoints/PedidosEndpoints.cs                                         │
│  ├── POST /api/pedidos  → IPedidoService.RegistrarPedidoAsync()        │
│  └── GET  /api/pedidos  → IPedidoService.ObtenerTodosAsync()           │
└─────────────────────────────┬──────────────────────────────────────────┘
                              │ depende de (interfaces)
┌─────────────────────────────▼──────────────────────────────────────────┐
│                      PedidosAPI.Application                            │
│         Casos de uso · DTOs · Validaciones · Excepciones               │
│                                                                        │
│  UseCases/RegistrarPedidoUseCase.cs                                    │
│  ├── 1. Validar request (FluentValidation)                             │
│  ├── 2. BeginTransaction (IUnitOfWork)                                 │
│  ├── 3. Log auditoría: PEDIDO_INICIO                                   │
│  ├── 4. Validar cliente (IValidacionClienteService)                    │
│  ├── 5. Persistir pedido (IPedidoRepository)                           │
│  ├── 6. Log auditoría: PEDIDO_CONFIRMADO                               │
│  └── 7. CommitAsync / RollbackAsync                                    │
└──────────────┬─────────────────────────────┬──────────────────────────┘
               │ define interfaces            │ implementa interfaces
┌──────────────▼────────────┐   ┌────────────▼───────────────────────────┐
│    PedidosAPI.Domain      │   │      PedidosAPI.Infrastructure          │
│  (sin dependencias)       │   │  EF Core · Polly · HttpClient           │
│                           │   │                                         │
│  Entities/                │   │  Data/AppDbContext.cs                   │
│  ├── PedidoCabecera       │   │  Data/UnitOfWork.cs                     │
│  ├── PedidoDetalle        │   │  Repositories/PedidoRepository          │
│  └── LogAuditoria         │   │  Repositories/AuditoriaRepository       │
│                           │   │  ExternalServices/                      │
│  Interfaces/              │   │  └── ValidacionClienteService           │
│  ├── IPedidoRepository    │   │      + Pipeline Polly:                  │
│  └── IAuditoriaRepository │   │        Timeout → CircuitBreaker         │
│                           │   │        → Retry → Timeout/intento        │
└───────────────────────────┘   └────────────────────────────────────────┘
```

### Principio de Dependencia

```
API  →  Application  →  Domain  ←  Infrastructure
```

- **Domain** no conoce a nadie
- **Application** solo conoce a Domain
- **Infrastructure** implementa las interfaces de Domain y Application
- **API** orquesta todo y configura el contenedor de DI

---

## 📁 Estructura del Proyecto

```
PedidosAPI/
├── PedidosAPI.sln
├── .gitignore
├── README.md
├── database/
│   └── script.sql                         ← DDL completo SQL Server
└── src/
    ├── PedidosAPI.Domain/
    │   ├── Entities/
    │   │   ├── PedidoCabecera.cs
    │   │   ├── PedidoDetalle.cs
    │   │   └── LogAuditoria.cs
    │   └── Interfaces/
    │       └── IRepositories.cs
    │
    ├── PedidosAPI.Application/
    │   ├── DTOs/
    │   │   └── PedidoDtos.cs
    │   ├── Exceptions/
    │   │   └── DomainExceptions.cs
    │   ├── Interfaces/
    │   │   └── IServices.cs
    │   ├── UseCases/
    │   │   └── RegistrarPedidoUseCase.cs
    │   └── Validators/
    │       └── CrearPedidoRequestValidator.cs
    │
    ├── PedidosAPI.Infrastructure/
    │   ├── Data/
    │   │   ├── AppDbContext.cs
    │   │   └── UnitOfWork.cs
    │   ├── DependencyInjection/
    │   │   └── InfrastructureServiceExtensions.cs
    │   ├── ExternalServices/
    │   │   └── ValidacionClienteService.cs
    │   └── Repositories/
    │       └── Repositories.cs
    │
    └── PedidosAPI.API/
        ├── DependencyInjection/
        │   └── ApplicationServiceExtensions.cs
        ├── Endpoints/
        │   └── PedidosEndpoints.cs
        ├── Middleware/
        │   └── ExceptionMiddleware.cs
        ├── Properties/
        │   └── launchSettings.json
        ├── Program.cs
        ├── appsettings.json
        └── appsettings.Development.json
```

---

## 🔍 Proyectos en Detalle

### PedidosAPI.Domain

**Responsabilidad:** Núcleo del negocio. No tiene ninguna dependencia externa. Define las entidades y los contratos que deben cumplir los repositorios.

#### Entidades

**`PedidoCabecera`** — Representa la cabecera del pedido:
```csharp
public class PedidoCabecera
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Usuario { get; set; }
    public ICollection<PedidoDetalle> Detalles { get; set; }
}
```

**`PedidoDetalle`** — Cada línea de producto en el pedido. Incluye `Subtotal` como propiedad calculada (no persiste en BD):
```csharp
public decimal Subtotal => Cantidad * Precio;
```

**`LogAuditoria`** — Registro de eventos del sistema. Tiene un campo `Nivel` que puede ser `INFO`, `WARNING` o `ERROR`.

#### Interfaces de Repositorio

Define los contratos sin conocer la implementación:
```csharp
public interface IPedidoRepository
{
    Task<PedidoCabecera> CrearAsync(PedidoCabecera pedido, CancellationToken ct);
    Task<IEnumerable<PedidoCabecera>> ObtenerTodosAsync(CancellationToken ct);
}
```

---

### PedidosAPI.Application

**Responsabilidad:** Lógica de negocio, casos de uso, validaciones y contratos de servicios externos. No sabe nada de EF Core, SQL, ni HTTP.

#### DTOs (Data Transfer Objects)

Separan el modelo de dominio de la API. El cliente nunca recibe entidades directamente:

| DTO | Dirección | Uso |
|-----|-----------|-----|
| `CrearPedidoRequest` | Entrada | Body del POST |
| `ItemPedidoDto` | Entrada | Cada producto del pedido |
| `CrearPedidoResponse` | Salida | Respuesta del POST |
| `PedidoResumenDto` | Salida | Item de la lista GET |
| `ListaPedidosResponse` | Salida | Respuesta del GET |

#### Excepciones de Dominio

Excepciones tipadas que representan errores de negocio:

```csharp
PedidoInvalidoException     → datos del request no válidos      → HTTP 400
ClienteNoValidoException    → cliente no pasó validación externa → HTTP 422
ServicioExternoException    → fallo en el servicio HTTP externo  → HTTP 503
```

#### Validaciones con FluentValidation

`CrearPedidoRequestValidator` valida de forma declarativa antes de ejecutar el caso de uso:
- `ClienteId` mayor a 0
- `Usuario` no vacío, máximo 100 caracteres
- `Items` con al menos un elemento
- Cada item: `ProductoId`, `Cantidad` y `Precio` mayores a 0

#### Caso de Uso: RegistrarPedidoUseCase

Orquesta el flujo completo en 9 pasos dentro de una transacción:

```
1. Validar request (FluentValidation)
2. Abrir transacción (IUnitOfWork.BeginTransactionAsync)
3. Registrar auditoría: PEDIDO_INICIO
4. Llamar servicio externo (IValidacionClienteService)
   ├── Error de red   → log ERROR → ROLLBACK → ServicioExternoException
   └── Cliente 404   → log WARNING → ROLLBACK → ClienteNoValidoException
5. Construir entidades PedidoCabecera + PedidoDetalle[]
6. Persistir (IPedidoRepository.CrearAsync)
7. Registrar auditoría: PEDIDO_CONFIRMADO
8. Confirmar transacción (IUnitOfWork.CommitAsync)
9. Mapear y retornar CrearPedidoResponse
```

---

### PedidosAPI.Infrastructure

**Responsabilidad:** Implementaciones concretas. EF Core, repositorios, comunicación HTTP con Polly. Esta capa conoce SQL Server, HttpClient y todos los detalles técnicos.

#### AppDbContext

Configura el mapeo ORM con Fluent API:
- Nombres de tabla explícitos (`PedidoCabecera`, `PedidoDetalle`, `LogAuditoria`)
- Columnas `decimal(18,2)` para precios y totales
- `GETUTCDATE()` como valor por defecto en fechas
- `Subtotal` ignorado (es propiedad calculada)
- FK con `CASCADE` entre `PedidoCabecera` → `PedidoDetalle`
- Índices en `ClienteId`, `Fecha`, `ProductoId` y `Nivel`

#### UnitOfWork

Abstrae las transacciones de EF Core:
```csharp
await unitOfWork.BeginTransactionAsync();  // IDbContextTransaction
await unitOfWork.CommitAsync();            // SaveChanges + CommitTransaction
await unitOfWork.RollbackAsync();          // RollbackTransaction
```

**Nota técnica importante:** `EnableRetryOnFailure` de SQL Server es incompatible con transacciones manuales. Por eso se usa `UseSqlServer` sin esa opción, y la resiliencia se delega a Polly en la capa HTTP.

#### Repositorios

- **PedidoRepository:** `CrearAsync` agrega la entidad al context (sin `SaveChanges`, lo hace el UnitOfWork). `ObtenerTodosAsync` usa `Include + AsNoTracking + OrderByDescending`.
- **AuditoriaRepository:** `RegistrarAsync` agrega el log al mismo context, por lo que entra en la misma transacción del pedido.

#### ValidacionClienteService

Consume `https://jsonplaceholder.typicode.com/users/{clienteId}` con el pipeline de Polly ya configurado. Atrapa `BrokenCircuitException` para convertirla en `ServicioExternoException` con mensaje claro.

#### Pipeline de Resiliencia con Polly

```
REQUEST
   │
   ▼ [1] Timeout total: 10 seg
   │     Si toda la operación supera 10 seg → TimeoutRejectedException
   │
   ▼ [2] Circuit Breaker
   │     - 3 fallos consecutivos → ABIERTO 15 seg
   │     - ABIERTO: BrokenCircuitException inmediata (sin llamada real)
   │     - Tras 15 seg → SEMI-ABIERTO (prueba)
   │     - Prueba exitosa → CERRADO
   │
   ▼ [3] Retry: 3 reintentos con backoff exponencial + jitter
   │     Esperas: ~2s → ~4s → ~8s
   │
   ▼ [4] Timeout por intento: 5 seg
   │
   ▼ httpClient.GetAsync("users/{id}")
```

---

### PedidosAPI.API

**Responsabilidad:** Punto de entrada. Registra servicios, configura el pipeline HTTP, define los endpoints con Minimal API y expone la documentación con Scalar.

#### Program.cs

Orquesta todo en orden:
```csharp
builder.Services.AddApplication();       // casos de uso + validadores
builder.Services.AddInfrastructure();    // EF + repos + Polly + HttpClient
builder.Services.AddOpenApi();           // spec OpenAPI nativo .NET 9

app.UseMiddleware<ExceptionMiddleware>(); // manejo global de errores
app.MapOpenApi();                        // /openapi/v1.json
app.MapScalarApiReference();             // /scalar/v1
app.MapGet("/", redirect → /scalar/v1); // raíz redirige a Scalar
app.MapPedidosEndpoints();              // endpoints del negocio
```

#### Minimal API Endpoints

Los endpoints se agrupan en `PedidosEndpoints.cs`:

```csharp
var group = app.MapGroup("/api/pedidos").WithTags("Pedidos").WithOpenApi();

group.MapPost("/", RegistrarPedido)    // POST /api/pedidos
group.MapGet("/",  ObtenerTodos)       // GET  /api/pedidos
```

Ventajas de Minimal API vs Controllers:
- Menos boilerplate
- Más explícito y trazable
- Mejor integración con OpenAPI nativo de .NET 9
- Los handlers son funciones simples, fáciles de testear

#### ExceptionMiddleware

Intercepta todas las excepciones no controladas y las convierte en respuestas HTTP con formato JSON consistente:

```json
{
  "status": 503,
  "error": "ServiceUnavailable",
  "mensaje": "El servicio de validación no está disponible.",
  "timestamp": "2024-02-20T10:30:00Z"
}
```

#### Scalar

Scalar es la UI moderna de documentación de API que reemplaza a Swagger UI. Se configura con:
- **Tema:** Purple
- **Cliente por defecto:** C# HttpClient
- **URL:** `/scalar/v1`
- La raíz `/` redirige automáticamente a `/scalar/v1`

---

## 🎯 Patrones y Decisiones de Diseño

| Patrón | Dónde | Por qué |
|--------|-------|---------|
| **Clean Architecture** | Toda la solución | Separación de responsabilidades, testeable, mantenible |
| **Repository Pattern** | Infrastructure | Abstrae el acceso a datos del caso de uso |
| **Unit of Work** | Infrastructure | Controla la transacción que envuelve múltiples repos |
| **Use Case / Interactor** | Application | Encapsula un caso de uso de negocio completo |
| **Circuit Breaker** | Infrastructure/Polly | Protege ante fallos del servicio externo |
| **Retry con Backoff** | Infrastructure/Polly | Reintenta ante fallos transitorios |
| **Middleware Pipeline** | API | Manejo global de errores centralizado |
| **DTOs + Records** | Application | Inmutabilidad, separación del modelo de dominio |
| **FluentValidation** | Application | Validaciones declarativas y reutilizables |

---

## ⚡ Circuit Breaker con Polly

### Estados y Transiciones

```
         3 fallos consecutivos
CLOSED ─────────────────────────► OPEN
  ▲                                  │
  │  prueba exitosa     15 segundos  │
  │                                  ▼
HALF-OPEN ◄──────────────────── OPEN
  │
  └── prueba fallida ──────────► OPEN
```

### Comportamiento por estado

| Estado | Qué pasa con las llamadas | Log en consola |
|--------|--------------------------|----------------|
| **CLOSED** | Pasan normalmente al servicio | _(sin mensaje)_ |
| **OPEN** | `BrokenCircuitException` inmediata, sin red | `[CIRCUIT BREAKER] ABIERTO durante 15s` |
| **HALF-OPEN** | Una llamada de prueba pasa | `[CIRCUIT BREAKER] SEMI-ABIERTO` |

### Qué cuenta como fallo

- `HttpRequestException` (red caída, DNS, conexión rechazada)
- `TimeoutRejectedException` (timeout de Polly)
- Respuestas HTTP 5xx (500, 502, 503, 504...)
- **NO cuenta:** HTTP 404 (cliente no encontrado es respuesta válida)
- **NO cuenta:** HTTP 200 (éxito)

---

## 🔒 Manejo Transaccional

Todo el proceso de registro de un pedido se ejecuta dentro de **una única transacción SQL Server**:

```
BeginTransaction()
    │
    ├── INSERT LogAuditoria (PEDIDO_INICIO)
    ├── [llamada HTTP externa — fuera de la transacción SQL]
    ├── INSERT PedidoCabecera
    ├── INSERT PedidoDetalle × N
    └── INSERT LogAuditoria (PEDIDO_CONFIRMADO)
         │
         ▼
    SaveChanges() + CommitTransaction()
         │
         └── Si hay error en cualquier paso:
             RollbackTransaction()
             → NADA se persiste en la base de datos
```

**Punto importante:** Los logs de auditoría forman parte de la misma transacción. Si hay rollback, los logs también se revierten. Esto garantiza que no quede información parcial o inconsistente.

---

## 📊 Registro de Eventos (Logging)

El proyecto usa dos mecanismos en paralelo:

### ILogger (consola)

Nativo de .NET, aparece en la consola de Visual Studio durante la ejecución:

```
info: Iniciando registro de pedido. ClienteId=1 Usuario=usuario.prueba
info: Validando ClienteId=1 con servicio externo.
warn: [RETRY] Intento #1 de 3. Esperando 2.3s...
info: ClienteId=1 validado correctamente.
info: Pedido #5 confirmado. Total=40,00
```

### LogAuditoria (base de datos)

Registro permanente en la tabla `LogAuditoria`, dentro de la misma transacción del pedido:

| Evento | Nivel | Cuándo ocurre |
|--------|-------|---------------|
| `PEDIDO_INICIO` | INFO | Al comenzar el proceso |
| `VALIDACION_ERROR` | ERROR | Si el servicio externo falla |
| `CLIENTE_INVALIDO` | WARNING | Si el cliente no existe (404) |
| `PEDIDO_CONFIRMADO` | INFO | Al completar exitosamente |

---

## 🚨 Manejo de Errores

### Tabla de errores y respuestas HTTP

| Situación | Excepción lanzada | HTTP | Mensaje |
|-----------|-------------------|------|---------|
| Request con datos inválidos | `PedidoInvalidoException` | `400 Bad Request` | Detalle de validación |
| Cliente no existe en servicio externo | `ClienteNoValidoException` | `422 Unprocessable Entity` | "El cliente X no superó la validación" |
| Servicio externo caído / circuit abierto | `ServicioExternoException` | `503 Service Unavailable` | "Servicio no disponible" |
| Cualquier otra excepción | `Exception` genérica | `500 Internal Server Error` | "Error interno inesperado" |

### Formato de respuesta de error

```json
{
  "status": 400,
  "error": "BadRequest",
  "mensaje": "ClienteId debe ser mayor a 0; La cantidad debe ser mayor a 0.",
  "timestamp": "2024-02-20T10:30:00Z"
}
```

---

## 🗄️ Base de Datos

### Diagrama de tablas

```
PedidoCabecera                    PedidoDetalle
──────────────                    ─────────────
Id         INT IDENTITY PK   ◄──  PedidoId   INT FK
ClienteId  INT NOT NULL           Id         INT IDENTITY PK
Fecha      DATETIME2              ProductoId INT
Total      DECIMAL(18,2)          Cantidad   INT
Usuario    NVARCHAR(100)          Precio     DECIMAL(18,2)

LogAuditoria
────────────
Id          INT IDENTITY PK
Fecha       DATETIME2
Evento      NVARCHAR(100)
Descripcion NVARCHAR(500)
Usuario     NVARCHAR(100)
Nivel       NVARCHAR(10)       ← INFO | WARNING | ERROR
```

### Crear la base de datos

```bash
sqlcmd -S localhost -U sa -P "TuPassword" -i database/script.sql
```

O con EF Migrations:
```bash
cd src/PedidosAPI.API
dotnet ef migrations add InitialCreate --project ../PedidosAPI.Infrastructure
dotnet ef database update
```

---

## 🚀 Endpoints

### POST /api/pedidos — Registrar pedido

**Request:**
```json
{
  "clienteId": 1,
  "usuario": "usuario.prueba",
  "items": [
    { "productoId": 1, "cantidad": 2, "precio": 10 },
    { "productoId": 2, "cantidad": 1, "precio": 20 }
  ]
}
```

**Response 201 Created:**
```json
{
  "pedidoId": 1,
  "clienteId": 1,
  "usuario": "usuario.prueba",
  "fecha": "2024-02-20T10:30:00Z",
  "total": 40.00,
  "items": [
    { "productoId": 1, "cantidad": 2, "precio": 10.00, "subtotal": 20.00 },
    { "productoId": 2, "cantidad": 1, "precio": 20.00, "subtotal": 20.00 }
  ]
}
```

### GET /api/pedidos — Listar todos los pedidos

**Response 200 OK:**
```json
{
  "totalRegistros": 2,
  "pedidos": [
    {
      "pedidoId": 2,
      "clienteId": 1,
      "usuario": "juan.perez",
      "fecha": "2024-02-20T11:00:00Z",
      "total": 60.00,
      "totalItems": 3
    },
    {
      "pedidoId": 1,
      "clienteId": 1,
      "usuario": "usuario.prueba",
      "fecha": "2024-02-20T10:30:00Z",
      "total": 40.00,
      "totalItems": 2
    }
  ]
}
```

---

## ⚙️ Configuración y Ejecución

### Requisitos
- .NET 9 SDK
- SQL Server 2019+ (o Docker: `docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`)

### Connection String

`src/PedidosAPI.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PedidosDB;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;"
  }
}
```

### Ejecutar

```bash
cd src/PedidosAPI.API
dotnet run
```

### URLs disponibles

| URL | Descripción |
|-----|-------------|
| `http://localhost:5178/` | Redirige a Scalar |
| `http://localhost:5178/scalar/v1` | UI interactiva Scalar |
| `http://localhost:5178/openapi/v1.json` | Spec OpenAPI JSON |
| `http://localhost:5178/api/pedidos` | Endpoint de pedidos |

---

## 📦 Paquetes NuGet

| Paquete | Versión | Proyecto | Uso |
|---------|---------|----------|-----|
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.0 | Infrastructure | ORM + SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.0 | Infrastructure | Migraciones |
| `Polly` | 8.4.1 | Infrastructure | Circuit Breaker, Retry, Timeout |
| `Polly.Extensions.Http` | 3.0.0 | Infrastructure | Integración Polly + HttpClient |
| `Microsoft.Extensions.Http.Polly` | 9.0.0 | Infrastructure | `AddPolicyHandler` |
| `Microsoft.AspNetCore.OpenApi` | 9.0.0 | API | Spec OpenAPI nativo .NET 9 |
| `Scalar.AspNetCore` | 1.9.177 | API | UI interactiva de documentación |
| `FluentValidation` | 11.9.0 | Application | Validaciones declarativas |
| `FluentValidation.DependencyInjectionExtensions` | 11.9.0 | API | Registro DI de validadores |

---

## ✅ Criterios de Evaluación

### Correcto Funcionamiento
Los dos endpoints funcionan correctamente. El POST valida, registra y retorna 201 con Location header. El GET retorna todos los pedidos ordenados por fecha descendente.

### Calidad del Código
- Uso de `record` para DTOs (inmutabilidad)
- Primary constructors en clases de infraestructura
- Nomenclatura en español consistente con el dominio
- Métodos cortos con responsabilidad única
- Sin magic strings (excepciones tipadas con mensajes en el constructor)

### Diseño de la Solución
Clean Architecture con 4 proyectos. Las dependencias apuntan hacia el Domain. El API no conoce a Infrastructure directamente. Los contratos están en Application e implementados en Infrastructure.

### Manejo de Errores
`ExceptionMiddleware` centraliza el manejo. Tres excepciones de dominio tipadas. Cada excepción produce el HTTP status code semánticamente correcto. Circuit Breaker convierte `BrokenCircuitException` en `ServicioExternoException` antes de llegar al middleware.

### Uso de Transacciones
`UnitOfWork` envuelve `BeginTransaction + SaveChanges + Commit` en un método `CommitAsync`. El bloque `catch` en el use case garantiza `RollbackAsync` ante cualquier excepción. Pedido, detalles y logs de auditoría forman parte de la misma transacción.

### Orden y Claridad
Cada clase tiene una única responsabilidad. Los archivos están organizados en carpetas que reflejan su propósito. Los comentarios explican el "por qué" y no el "qué". El pipeline de Polly está documentado con diagramas ASCII en el código.

### Uso Correcto de Git
- Repositorio inicializado desde el primer commit
- Un commit por cada capa o característica significativa
- Mensajes de commit siguiendo convención `tipo(scope): descripción`

## 👤 Autor Alvaro Andrés Cárdenas Salazar

Desarrollado como prueba técnica práctica. Sistema transaccional de pedidos con patrones empresariales modernos.
