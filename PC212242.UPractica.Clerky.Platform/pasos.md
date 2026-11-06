# Clerky Platform — Guía de desarrollo y bitácora de la práctica

> **Propósito:** documentar la arquitectura, el código revisado, la configuración, los comandos y las pruebas realizadas en el proyecto de práctica Clerky.  
> **Proyecto:** `PC212242.UPractica.Clerky.Platform`  
> **Tecnologías:** C# / .NET 10, ASP.NET Core, Entity Framework Core 9, Pomelo para MySQL 8, OpenAPI.  
> **Endpoint implementado:** `POST /api/v1/contracts`.

## 1. Ubicación del proyecto y archivos

Ruta de la solución en Windows:

```text
C:\Users\aikho\RiderProjects\pc212242upractica
└── PC212242.UPractica.Clerky.Platform
    ├── Properties/
    ├── Controllers/
    ├── Migrations/
    ├── Paperwork/
    │   ├── Domain/
    │   │   ├── Model/
    │   │   │   ├── Aggregates/Contract.cs
    │   │   │   ├── Commands/CreateContractCommand.cs
    │   │   │   ├── Enumerations/ContractStatus.cs
    │   │   │   └── ValueObjects/{ClerkyIdentifier,PartyId}.cs
    │   │   └── Repositories/IContractRepository.cs
    │   ├── Application/
    │   │   ├── Services/IContractCommandService.cs
    │   │   └── Internal/CommandServices/ContractCommandService.cs
    │   ├── Infrastructure/Persistence/EFC/
    │   │   ├── Configuration/ContractConfiguration.cs
    │   │   └── Repositories/ContractRepository.cs
    │   └── Interfaces/REST/
    │       ├── Controllers/ContractsController.cs
    │       ├── Resources/{CreateContractResource,ContractResource,ContractErrorResource}.cs
    │       └── Transform/{CreateContractCommandFromResourceAssembler,ContractResourceFromEntityAssembler}.cs
    ├── Resources/
    ├── Shared/
    │   ├── Domain/Model/ValueObjects/{ValidityPeriod,MonetaryAmount}.cs
    │   └── Infrastructure/Persistence/EFC/AppDbContext.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Program.cs
    ├── README.md
    └── pasos.md
```

La estructura anterior combina las carpetas visibles en Rider y las rutas de los archivos revisados. Las carpetas `Controllers/` y `Resources/` de la raíz también aparecen en Rider, pero el endpoint de Clerky está organizado en `Paperwork/Interfaces/REST/`.

## 2. Requisitos funcionales

El sistema permite crear contratos mediante **un único endpoint POST**. El agregado `Contract` tiene:

- `Id`: entero, llave primaria generada por la base de datos.
- `DocumentIdentifier`: Value Object `ClerkyIdentifier`, encapsula un `Guid` y debe ser único.
- `ClientId`: Value Object `PartyId`, encapsula un `Guid`.
- `Period`: Value Object compartido `ValidityPeriod`, con `StartDate` y `EndDate`.
- `TotalValue`: Value Object compartido `MonetaryAmount`, con `Amount` y `Currency`.
- `Status`: `Draft` al crear; también existen `Active` y `Expired`.
- `Notes`: cadena opcional.
- `CreatedAt`, `UpdatedAt`: marcas temporales de auditoría.

Reglas principales:

1. No permitir otro contrato con el mismo `DocumentIdentifier`.
2. `EndDate` debe ser estrictamente posterior a `StartDate`.
3. `MonetaryAmount` no puede ser negativo; debe lanzar `ArgumentOutOfRangeException`.
4. La moneda no puede ser nula, vacía ni solo espacios; según el enunciado debe lanzar `ArgumentNullException`.
5. El contrato se crea con estado `Draft`.
6. Los errores deben comunicarse mediante códigos HTTP y mensajes en inglés o español según `Accept-Language`.

## 3. Arquitectura DDD y CQRS

```text
Cliente HTTP
   |
   v
POST /api/v1/contracts
   |
   v
ContractsController
   |
   v
CreateContractResource
   |
   v
CreateContractCommandFromResourceAssembler
   |
   v
CreateContractCommand
   |
   v
IContractCommandService -> ContractCommandService
   |
   v
IContractRepository -> ContractRepository
   |
   v
AppDbContext + ContractConfiguration
   |
   v
MySQL (clerky_wa, tabla contracts)
   |
   v
ContractResourceFromEntityAssembler
   |
   v
HTTP 201 + ContractResource
```

- **Domain:** agregado, reglas de negocio, Value Objects, enumeración, comando y contrato de repositorio.
- **Application:** servicio que verifica duplicados y coordina la creación.
- **Infrastructure:** EF Core, Fluent API y repositorio.
- **Interfaces/REST:** controlador, DTOs/Resources y Assemblers.
- **Shared:** Value Objects reutilizables y `AppDbContext`.

## 4. Código del dominio

### 4.1. `ContractStatus.cs`

```csharp
namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Enumerations;

public enum ContractStatus
{
    Draft,
    Active,
    Expired
}
```

### 4.2. `ClerkyIdentifier.cs`

```csharp
namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.ValueObjects;

public record ClerkyIdentifier
{
    public Guid Value { get; init; }

    public ClerkyIdentifier(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Document identifier cannot be empty.");

        Value = value;
    }
}
```

### 4.3. `PartyId.cs`

```csharp
namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.ValueObjects;

public record PartyId
{
    public Guid Value { get; init; }

    public PartyId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Client identifier cannot be empty.");

        Value = value;
    }
}
```

### 4.4. `ValidityPeriod.cs`

```csharp
namespace PC212242.UPractica.Clerky.Platform.Shared.Domain.Model.ValueObjects;

public record ValidityPeriod
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    public ValidityPeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException(
                "End date must be later than start date.");

        StartDate = startDate;
        EndDate = endDate;
    }

    public bool IsExpired(DateOnly checkDate)
    {
        return checkDate > EndDate;
    }
}
```

### 4.5. `MonetaryAmount.cs` (versión corregida)

```csharp
namespace PC212242.UPractica.Clerky.Platform.Shared.Domain.Model.ValueObjects;

/// <summary>
/// Represents a monetary amount and its currency.
/// </summary>
/// <remarks>
/// Author: Andrea Correa Rodríguez.
/// </remarks>
public record MonetaryAmount
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public MonetaryAmount(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Monetary amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentNullException(
                nameof(currency),
                "Currency cannot be empty.");

        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Multiplies the monetary amount by a factor.
    /// </summary>
    /// <param name="factor">The multiplication factor.</param>
    /// <returns>A new monetary amount with the same currency.</returns>
    public MonetaryAmount Multiply(decimal factor)
    {
        return new MonetaryAmount(Amount * factor, Currency);
    }
}
```

**Cambio importante:** al principio se usaba `ArgumentException` en ambos casos. Se corrigieron los tipos para cumplir el enunciado. El mensaje inglés de `ArgumentOutOfRangeException` incluye automáticamente `(Parameter 'amount')`.

### 4.6. `Contract.cs`

```csharp
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Enumerations;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.ValueObjects;
using PC212242.UPractica.Clerky.Platform.Shared.Domain.Model.ValueObjects;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;

public class Contract
{
    public int Id { get; private set; }
    public ClerkyIdentifier DocumentIdentifier { get; private set; }
    public PartyId ClientId { get; private set; }
    public ValidityPeriod Period { get; private set; }
    public MonetaryAmount TotalValue { get; private set; }
    public ContractStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Contract()
    {
        DocumentIdentifier = null!;
        ClientId = null!;
        Period = null!;
        TotalValue = null!;
    }

    public Contract(Guid documentIdentifier, Guid clientId, DateOnly startDate,
        DateOnly endDate, decimal amount, string currency, string? notes)
    {
        DocumentIdentifier = new ClerkyIdentifier(documentIdentifier);
        ClientId = new PartyId(clientId);
        Period = new ValidityPeriod(startDate, endDate);
        TotalValue = new MonetaryAmount(amount, currency);
        Status = ContractStatus.Draft;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 4.7. `CreateContractCommand.cs`

```csharp
namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Commands;

public record CreateContractCommand(
    Guid DocumentIdentifier,
    Guid ClientId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Amount,
    string Currency,
    string? Notes
);
```

## 5. Capa de aplicación

### `ContractCommandService.cs`

```csharp
using PC212242.UPractica.Clerky.Platform.Paperwork.Application.Services;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Commands;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Repositories;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Application.Internal.CommandServices;

public class ContractCommandService(
    IContractRepository contractRepository
) : IContractCommandService
{
    public async Task<Contract> Handle(CreateContractCommand command)
    {
        var exists = await contractRepository
            .ExistsByDocumentIdentifierAsync(command.DocumentIdentifier);
        if (exists)
            throw new InvalidOperationException(
                "A contract with this document identifier already exists.");

        var contract = new Contract(
            command.DocumentIdentifier, command.ClientId, command.StartDate,
            command.EndDate, command.Amount, command.Currency, command.Notes
        );
        await contractRepository.AddAsync(contract);
        return contract;
    }
}
```

La interfaz `IContractCommandService` se utilizó y registró en DI; su texto completo no fue compartido en la revisión. El servicio de aplicación **no** realiza SQL: delega en el repositorio.

## 6. REST: Resources y Assemblers

### 6.1. `CreateContractResource.cs` (versión corregida)

```csharp
namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

/// <summary>
/// Represents the information required to create a contract.
/// </summary>
/// <remarks>Author: Andrea Correa Rodríguez.</remarks>
public record CreateContractResource(
    string DocumentId,
    string ClientId,
    string PeriodStartDate,
    string PeriodEndDate,
    decimal MonetaryAmountValue,
    string MonetaryAmountCurrency,
    string? Notes = null
);
```

### 6.2. `ContractResource.cs`

```csharp
namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

public record ContractResource(
    int Id,
    string DocumentId,
    string ClientId,
    string PeriodStartDate,
    string PeriodEndDate,
    decimal MonetaryAmountValue,
    string MonetaryAmountCurrency
);
```

### 6.3. `ContractErrorResource.cs` (archivo agregado)

```csharp
namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

/// <summary>
/// Represents an API error response.
/// </summary>
/// <remarks>Author: Andrea Correa Rodríguez.</remarks>
public record ContractErrorResource(string Message);
```

### 6.4. `CreateContractCommandFromResourceAssembler.cs` (versión corregida)

```csharp
using System.Globalization;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Commands;
using PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Transform;

/// <summary>Converts contract creation resources into commands.</summary>
/// <remarks>Author: Andrea Correa Rodríguez.</remarks>
public static class CreateContractCommandFromResourceAssembler
{
    /// <summary>Creates a contract command from a REST resource.</summary>
    public static CreateContractCommand ToCommandFromResource(
        CreateContractResource resource)
    {
        return new CreateContractCommand(
            Guid.Parse(resource.DocumentId),
            Guid.Parse(resource.ClientId),
            DateOnly.ParseExact(resource.PeriodStartDate, "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            DateOnly.ParseExact(resource.PeriodEndDate, "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            resource.MonetaryAmountValue,
            resource.MonetaryAmountCurrency,
            resource.Notes
        );
    }
}
```

**Cambios:** se añadió `Notes` opcional y se reemplazó `DateOnly.Parse` por `DateOnly.ParseExact` para exigir fechas `yyyy-MM-dd`.

### 6.5. `ContractResourceFromEntityAssembler.cs`

```csharp
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Transform;

public static class ContractResourceFromEntityAssembler
{
    public static ContractResource ToResourceFromEntity(Contract contract)
    {
        return new ContractResource(
            contract.Id,
            contract.DocumentIdentifier.Value.ToString(),
            contract.ClientId.Value.ToString(),
            contract.Period.StartDate.ToString("yyyy-MM-dd"),
            contract.Period.EndDate.ToString("yyyy-MM-dd"),
            contract.TotalValue.Amount,
            contract.TotalValue.Currency
        );
    }
}
```

### 6.6. `ContractsController.cs` (versión corregida, sin advertencia CS0168)

```csharp
using Microsoft.AspNetCore.Mvc;
using PC212242.UPractica.Clerky.Platform.Paperwork.Application.Services;
using PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;
using PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Transform;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Controllers;

/// <summary>Provides REST operations for contract management.</summary>
/// <remarks>Author: Andrea Correa Rodríguez.</remarks>
[ApiController]
[Route("api/v1/contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractCommandService _contractCommandService;

    public ContractsController(IContractCommandService contractCommandService)
    {
        _contractCommandService = contractCommandService;
    }

    /// <summary>Creates a new contract.</summary>
    /// <param name="resource">Contract creation information.</param>
    /// <returns>The created contract.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ContractResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ContractErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ContractErrorResource), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateContract(
        [FromBody] CreateContractResource resource)
    {
        try
        {
            var command =
                CreateContractCommandFromResourceAssembler.ToCommandFromResource(resource);
            var contract = await _contractCommandService.Handle(command);
            var contractResource =
                ContractResourceFromEntityAssembler.ToResourceFromEntity(contract);
            return StatusCode(StatusCodes.Status201Created, contractResource);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ContractErrorResource(LocalizeMessage(exception)));
        }
        catch (FormatException)
        {
            return BadRequest(new ContractErrorResource(
                IsSpanish()
                    ? "El identificador o la fecha tienen un formato inválido."
                    : "The identifier or date has an invalid format."));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ContractErrorResource(LocalizeMessage(exception)));
        }
    }

    private bool IsSpanish()
    {
        var language = Request.Headers.AcceptLanguage.ToString();
        return language.StartsWith("es", StringComparison.OrdinalIgnoreCase);
    }

    private string LocalizeMessage(Exception exception)
    {
        if (!IsSpanish())
            return exception.Message;

        if (exception is ArgumentOutOfRangeException)
            return "El monto monetario no puede ser negativo.";

        if (exception is ArgumentNullException)
            return "La moneda no puede estar vacía.";

        return exception.Message switch
        {
            "End date must be later than start date." =>
                "La fecha de finalización debe ser posterior a la fecha de inicio.",
            "Document identifier cannot be empty." =>
                "El identificador del documento no puede estar vacío.",
            "Client identifier cannot be empty." =>
                "El identificador del cliente no puede estar vacío.",
            "A contract with this document identifier already exists." =>
                "Ya existe un contrato con este identificador de documento.",
            _ => exception.Message
        };
    }
}
```

**Nota:** la localización mostrada cubre los casos probados y los mensajes contemplados en el controlador; no sustituye una localización global de las respuestas automáticas de validación de ASP.NET Core.

## 7. Persistencia con EF Core

### 7.1. `ContractConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Infrastructure.Persistence.EFC.Configuration;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.OwnsOne(c => c.DocumentIdentifier, owned =>
        {
            owned.Property(v => v.Value)
                .HasColumnName("document_identifier")
                .HasConversion(value => value.ToString(), value => Guid.Parse(value))
                .HasMaxLength(36)
                .IsRequired();
            owned.HasIndex(v => v.Value).IsUnique();
        });

        builder.OwnsOne(c => c.ClientId, owned =>
        {
            owned.Property(v => v.Value)
                .HasColumnName("client_id")
                .HasConversion(value => value.ToString(), value => Guid.Parse(value))
                .HasMaxLength(36)
                .IsRequired();
        });

        builder.OwnsOne(c => c.Period, owned =>
        {
            owned.Property(v => v.StartDate)
                .HasColumnName("start_date").IsRequired();
            owned.Property(v => v.EndDate)
                .HasColumnName("end_date").IsRequired();
        });

        builder.OwnsOne(c => c.TotalValue, owned =>
        {
            owned.Property(v => v.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2).IsRequired();
            owned.Property(v => v.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3).IsRequired();
        });

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(c => c.Notes)
            .HasColumnName("notes").HasMaxLength(1000);
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at").IsRequired();
    }
}
```

### 7.2. `AppDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Infrastructure.Persistence.EFC.Configuration;

namespace PC212242.UPractica.Clerky.Platform.Shared.Infrastructure.Persistence.EFC;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Contract> Contracts => Set<Contract>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ContractConfiguration());
    }
}
```

### 7.3. `ContractRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Repositories;
using PC212242.UPractica.Clerky.Platform.Shared.Infrastructure.Persistence.EFC;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Infrastructure.Persistence.EFC.Repositories;

public class ContractRepository(AppDbContext context) : IContractRepository
{
    public async Task AddAsync(Contract contract)
    {
        await context.Contracts.AddAsync(contract);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByDocumentIdentifierAsync(Guid documentIdentifier)
    {
        return await context.Contracts.AnyAsync(
            contract => contract.DocumentIdentifier.Value == documentIdentifier);
    }
}
```

La interfaz `IContractRepository` se revisó a través de sus usos, pero no se compartió su contenido textual completo. Sus operaciones usadas son `AddAsync(Contract)` y `ExistsByDocumentIdentifierAsync(Guid)`.

## 8. Configuración de arranque: `Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PC212242.UPractica.Clerky.Platform.Paperwork.Application.Internal.CommandServices;
using PC212242.UPractica.Clerky.Platform.Paperwork.Application.Services;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Repositories;
using PC212242.UPractica.Clerky.Platform.Paperwork.Infrastructure.Persistence.EFC.Repositories;
using PC212242.UPractica.Clerky.Platform.Shared.Infrastructure.Persistence.EFC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);

builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IContractCommandService, ContractCommandService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.MapControllers();
app.Run();
```

El proyecto lee la cadena de conexión de `ConnectionStrings:DefaultConnection`. **No publiques credenciales reales** en el README, este documento ni el ZIP si no son necesarias.

Ejemplo de estructura de configuración (reemplazar con los valores locales):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=clerky_wa;user=TU_USUARIO;password=TU_PASSWORD"
  }
}
```

La conexión a MySQL 8 fue configurada y las migraciones fueron creadas y aplicadas durante la práctica; no se compartieron los nombres exactos de las migraciones.

## 9. Comandos de terminal: paso a paso

### 9.1. Abrir PowerShell y entrar al proyecto

```powershell
cd C:\Users\aikho\RiderProjects\pc212242upractica\PC212242.UPractica.Clerky.Platform
```

**Importante:** si se ejecuta `dotnet run` desde `pc212242upractica` (la carpeta superior), puede salir `No se ha podido encontrar un proyecto para ejecutar`, porque el `.csproj` está dentro de `PC212242.UPractica.Clerky.Platform`.

Alternativa desde la carpeta superior:

```powershell
dotnet run --project .\PC212242.UPractica.Clerky.Platform\PC212242.UPractica.Clerky.Platform.csproj
```

### 9.2. Restaurar, limpiar y compilar

```powershell
dotnet clean
dotnet restore
dotnet build
```

- `dotnet clean`: elimina archivos generados de compilaciones anteriores.
- `dotnet restore`: restaura dependencias NuGet.
- `dotnet build`: compila el proyecto y muestra errores/advertencias.

**Resultado observado al final de la práctica:** `dotnet clean` correcto; `dotnet build` correcto en **3.5 s**, sin errores ni advertencias visibles.

### 9.3. Migraciones de EF Core

Para un proyecto nuevo, una vez configuradas las entidades y la conexión:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

`InitialCreate` es un **nombre de ejemplo**, no el nombre confirmado de la migración que se creó durante esta práctica. Si ya tienes la carpeta `Migrations/` y aplicaste las migraciones, **no vuelvas a crear `InitialCreate`**.

Comandos de inspección útiles:

```powershell
dotnet ef migrations list
dotnet ef dbcontext info
```

Si `dotnet ef` no está disponible, comprueba que la herramienta `dotnet-ef` esté instalada y que el proyecto tenga sus paquetes EF Core Design compatibles.

### 9.4. Iniciar servidor

```powershell
dotnet run
```

Salida confirmada:

```text
Now listening on: http://localhost:5055
Application started. Press Ctrl+C to shut down.
Hosting environment: Development
```

- API: `http://localhost:5055/api/v1/contracts`
- Especificación OpenAPI: `http://localhost:5055/openapi/v1.json`
- Para detener el servidor: `Ctrl + C`.
- **Deja abierta** la terminal del servidor y usa **otra** para las pruebas PowerShell.

## 10. Pruebas HTTP en PowerShell 5.1

### 10.1. Crear un contrato válido

```powershell
$body = @{
    documentId = [guid]::NewGuid().ToString()
    clientId = [guid]::NewGuid().ToString()
    periodStartDate = "2026-11-01"
    periodEndDate = "2026-12-01"
    monetaryAmountValue = 1500.00
    monetaryAmountCurrency = "USD"
    notes = "Practice contract"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://localhost:5055/api/v1/contracts" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

**Resultado observado:** se creó un contrato con `id: 2`, fechas `2026-11-01` a `2026-12-01`, importe `1500`, moneda `USD` y los siete campos esperados en la respuesta. El contrato se persistió correctamente según la respuesta de la API. El código HTTP exacto 201 estaba configurado en el controlador, aunque la salida de `Invoke-RestMethod` no mostró explícitamente el status en esta prueba.

### 10.2. Plantilla para inspeccionar errores HTTP

```powershell
try {
    Invoke-RestMethod `
        -Uri "http://localhost:5055/api/v1/contracts" `
        -Method POST `
        -ContentType "application/json" `
        -Headers @{"Accept-Language" = "es-PE"} `
        -Body $test
}
catch {
    Write-Host "HTTP:" ([int]$_.Exception.Response.StatusCode)
    $reader = New-Object System.IO.StreamReader(
        $_.Exception.Response.GetResponseStream()
    )
    Write-Host "Respuesta:" $reader.ReadToEnd()
}
```

### 10.3. Monto negativo (confirmado)

```powershell
$test = @{
    documentId = [guid]::NewGuid().ToString()
    clientId = [guid]::NewGuid().ToString()
    periodStartDate = "2026-11-01"
    periodEndDate = "2026-12-01"
    monetaryAmountValue = -100
    monetaryAmountCurrency = "USD"
} | ConvertTo-Json
```

Ejecuta la plantilla de errores anterior. **Resultado observado:**

```text
HTTP: 400
Respuesta: {"message":"El monto monetario no puede ser negativo."}
```

### 10.4. Documento duplicado (confirmado)

Se volvió a enviar el `documentId` ya registrado: `ed4faf45-1495-4e18-a0f7-6591acadf72f`.

```powershell
$duplicate = @{
    documentId = "ed4faf45-1495-4e18-a0f7-6591acadf72f"
    clientId = [guid]::NewGuid().ToString()
    periodStartDate = "2026-11-01"
    periodEndDate = "2026-12-01"
    monetaryAmountValue = 1500
    monetaryAmountCurrency = "USD"
} | ConvertTo-Json

try {
    Invoke-RestMethod `
        -Uri "http://localhost:5055/api/v1/contracts" `
        -Method POST `
        -ContentType "application/json" `
        -Headers @{"Accept-Language" = "es-PE"} `
        -Body $duplicate
}
catch {
    Write-Host "HTTP:" ([int]$_.Exception.Response.StatusCode)
    $reader = New-Object System.IO.StreamReader(
        $_.Exception.Response.GetResponseStream()
    )
    Write-Host "Respuesta:" $reader.ReadToEnd()
}
```

**Resultado observado:**

```text
HTTP: 409
Respuesta: {"message":"Ya existe un contrato con este identificador de documento."}
```

### 10.5. Tres pruebas automatizadas (confirmadas)

```powershell
$cases = @(
    @{
        Name = "Fecha final anterior"
        Start = "2026-12-01"
        End = "2026-11-01"
        Amount = 100
        Currency = "USD"
        Language = "es-PE"
    },
    @{
        Name = "Moneda vacia"
        Start = "2026-11-01"
        End = "2026-12-01"
        Amount = 100
        Currency = ""
        Language = "es-PE"
    },
    @{
        Name = "Monto negativo en ingles"
        Start = "2026-11-01"
        End = "2026-12-01"
        Amount = -100
        Currency = "USD"
        Language = "en-US"
    }
)

foreach ($case in $cases) {
    $body = @{
        documentId = [guid]::NewGuid().ToString()
        clientId = [guid]::NewGuid().ToString()
        periodStartDate = $case.Start
        periodEndDate = $case.End
        monetaryAmountValue = $case.Amount
        monetaryAmountCurrency = $case.Currency
    } | ConvertTo-Json

    Write-Host "`nPRUEBA:" $case.Name

    try {
        Invoke-RestMethod `
            -Uri "http://localhost:5055/api/v1/contracts" `
            -Method POST `
            -ContentType "application/json" `
            -Headers @{"Accept-Language" = $case.Language} `
            -Body $body
    }
    catch {
        Write-Host "HTTP:" ([int]$_.Exception.Response.StatusCode)
        $reader = New-Object System.IO.StreamReader(
            $_.Exception.Response.GetResponseStream()
        )
        Write-Host "Respuesta:" $reader.ReadToEnd()
    }
}
```

Resultados observados:

```text
PRUEBA: Fecha final anterior
HTTP: 400
Respuesta: {"message":"La fecha de finalización debe ser posterior a la fecha de inicio."}

PRUEBA: Moneda vacia
HTTP: 400
Respuesta: {"message":"La moneda no puede estar vacía."}

PRUEBA: Monto negativo en ingles
HTTP: 400
Respuesta: {"message":"Monetary amount cannot be negative. (Parameter 'amount')"}
```

## 11. Problemas que aparecieron y cómo se solucionaron

| Problema | Causa | Solución |
|---|---|---|
| `dotnet run`: no se encuentra proyecto | Se ejecutó desde la carpeta superior sin `.csproj` | Entrar a la subcarpeta del proyecto o usar `--project` |
| Advertencia `CS0168` en `ContractsController.cs` | `catch (FormatException exception)` declaraba variable no usada | Cambiar a `catch (FormatException)` |
| Excepciones incorrectas en `MonetaryAmount` | Se usaba `ArgumentException` genérica | Usar `ArgumentOutOfRangeException` y `ArgumentNullException` |
| `Notes` siempre era `null` | El request no tenía `Notes` y el Assembler pasaba `null` | Agregar campo opcional y transmitir `resource.Notes` |
| Fechas interpretadas con cultura variable | Se usaba `DateOnly.Parse` | Usar `DateOnly.ParseExact` con `yyyy-MM-dd` e `InvariantCulture` |
| Errores incompletos en OpenAPI | Se documentaban solo códigos | Agregar `ContractErrorResource` y especificar tipos de respuesta |
| Mensajes de validación sin traducir | Traducciones limitadas | Ampliar el mapeo de errores y contemplar `Accept-Language` |
| README sin autor | Faltaba sección `Author` | Agregar nombre completo oficial |

También se revisaron dependencias NuGet y se actualizaron previamente `Microsoft.OpenApi` y `MySqlConnector`; tras la actualización se informó una compilación correcta. Las versiones concretas reportadas fueron `Microsoft.OpenApi 2.7.5` y `MySqlConnector 2.6.2`.

## 12. README y documentación XML

El README oficial está en **inglés** e incluye: descripción, tecnologías, arquitectura, funcionalidades, endpoint, request de ejemplo, códigos HTTP, instrucciones de ejecución, OpenAPI y base de datos.

Agregar o verificar al final del README:

```markdown
## Author

Andrea Correa Rodríguez
```

> Sustituir por el nombre completo oficial que corresponda a la entrega.

El `.csproj` tiene habilitada la generación de documentación XML. **Esto no crea automáticamente comentarios**: las clases y métodos relevantes deben tener `/// <summary>` y, según el enunciado, `/// <remarks>` con el nombre completo. En la conversación se agregaron comentarios a varios archivos corregidos, pero **no se comprobó que todos los archivos restantes estén documentados**.

## 13. Revisión final y empaquetado

Checklist:

- [x] La API arranca en `http://localhost:5055`.
- [x] Se confirmó un contrato creado con ID generado.
- [x] Monto negativo devuelve 400 en español.
- [x] Documento duplicado devuelve 409 en español.
- [x] Fecha final anterior devuelve 400 en español.
- [x] Moneda vacía devuelve 400 en español.
- [x] Monto negativo devuelve 400 en inglés.
- [x] `dotnet build` final sin errores ni advertencias visibles.
- [ ] Confirmar comentarios XML completos y autor oficial.
- [ ] Confirmar que el README final incluye `Author`.
- [ ] Confirmar que el ZIP contiene migraciones y archivos fuente.
- [ ] Verificar nombre de ZIP conforme al NRC y código real de estudiante.
- [ ] Opcional: comprobar validación automática de ASP.NET Core, conflicto concurrente de índice único y cabeceras complejas `Accept-Language`.

### Crear ZIP sin `bin`, `obj` ni carpetas del IDE

Desde la carpeta superior `pc212242upractica`:

```powershell
cd C:\Users\aikho\RiderProjects\pc212242upractica

$project = ".\PC212242.UPractica.Clerky.Platform"
$staging = ".\entrega_temporal"

if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging | Out-Null

robocopy $project "$staging\PC212242.UPractica.Clerky.Platform" /E /XD bin obj .vs .idea /XF *.user

# Cambiar NRC y código de estudiante por los reales.
Compress-Archive -Path "$staging\PC212242.UPractica.Clerky.Platform" `
    -DestinationPath ".\pc2<NRC>u<CODIGO_ESTUDIANTE>.zip" -Force

Remove-Item $staging -Recurse -Force
```

**Atención:** en Windows no es válido usar literalmente `<` y `>` en nombres de archivo. Reemplaza **todo** `pc2<NRC>u<CODIGO_ESTUDIANTE>.zip` por el nombre definitivo antes de ejecutar. `robocopy` puede devolver códigos distintos de cero aunque haya copiado correctamente; revisa su resumen.

Si el enunciado exige incluir el archivo de solución `.sln` o `.slnx` de la carpeta superior, agrégalo al ZIP: el ejemplo anterior comprime solo la carpeta del proyecto.

## 14. Resumen rápido para estudiar

1. **DDD:** `Contract` protege invariantes mediante Value Objects.
2. **CQRS:** `CreateContractCommand` representa la intención de crear.
3. **Servicio de aplicación:** comprueba duplicados, construye agregado y solicita persistencia.
4. **Repositorio:** abstrae EF Core y ejecuta consultas asíncronas.
5. **Fluent API:** define tabla `contracts`, columnas `snake_case`, propiedades `OwnsOne` e índice único.
6. **REST:** el controlador recibe `CreateContractResource`, convierte mediante Assembler y devuelve `ContractResource`.
7. **Errores:** 400 para argumentos inválidos, 409 para documento duplicado, 201 para creación correcta.
8. **Localización:** `Accept-Language: es-PE` o `en-US` en los casos probados.
9. **Comandos clave:** `dotnet clean`, `dotnet restore`, `dotnet build`, `dotnet ef database update`, `dotnet run`.
10. **Entrega:** código fuente + migraciones + README + documentación, comprimidos con el nombre exigido.

---

**Alcance de esta bitácora:** se basa en los archivos y las salidas de terminal compartidos durante la práctica. Los archivos no mostrados íntegramente (como las interfaces, migraciones y configuración real de conexión) se describen por su función, sin inventar su código exacto. Los ejemplos de código son referencias de estudio; el estado definitivo siempre es el que esté guardado en Rider.
