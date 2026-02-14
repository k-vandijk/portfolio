# Architecture

This document describes the architectural patterns, design decisions, and project structure of the Portfolio Insight Dashboard.

## Clean Architecture Overview

The solution follows Clean Architecture principles with four distinct layers. Dependencies flow inward toward the domain:

```
┌─────────────────────────────────────────────────┐
│         Web (Presentation Layer)                │
│  Controllers, Views, ViewModels, Static Assets  │
└─────────────────┬───────────────────────────────┘
                  │
    ┌─────────────┴───────────────┐
    │                             │
    ▼                             ▼
┌─────────────────┐     ┌──────────────────────┐
│  Application    │     │  Infrastructure      │
│  Business Logic │     │  External Services   │
│  DTOs, Helpers  │     │  API, Azure Storage  │
└────────┬────────┘     └──────────┬───────────┘
         │                         │
         │         Domain          │
         └────────►  Core  ◄───────┘
                   Models
```

### Dependency Rules

- **Domain** (`Dashboard.Domain`) - Zero dependencies, contains pure models and constants
- **Application** (`Dashboard.Application`) - Depends only on Domain, defines interfaces
- **Infrastructure** (`Dashboard.Infrastructure`) - Depends on Domain, implements Application interfaces
- **Web** (`Dashboard._Web`) - Depends on Application, references Infrastructure for DI registration

### Layer Responsibilities

#### Domain Layer
**Location**: `src/Dashboard.Domain/`

Core business models and constants with no external dependencies.

**Contents**:
- `TransactionEntity` - Azure Table Storage entity model
- `StaticDetails` - Application-wide constants (cache durations, table names, partition key)

**Key File**: `src/Dashboard.Domain/Utils/StaticDetails.cs`

#### Application Layer
**Location**: `src/Dashboard.Application/`

Business logic, data transfer objects, and service interfaces.

**Contents**:
- **DTOs**: Data transfer objects for cross-layer communication
  - `TransactionDto` - Transaction data
  - `MarketHistoryResponseDto` - API response wrapper
  - `MarketHistoryDto` - Historical price data
- **Interfaces**: Service contracts
  - `ITickerApiService` - Market data fetching
  - `IAzureTableService` - Transaction persistence
- **Helpers**: Static utility classes
  - `FilterHelper` - Transaction and date filtering
  - `FormattingHelper` - Culture-aware parsing and formatting
  - `PeriodHelper` - Time range conversion
- **Mappers**: Entity/DTO conversion
  - `TransactionMapper` - Bidirectional Entity ↔ DTO mapping

#### Infrastructure Layer
**Location**: `src/Dashboard.Infrastructure/`

External service implementations and dependency injection setup.

**Contents**:
- **Services**:
  - `TickerApiService` - HTTP client for Ticker API with caching
  - `AzureTableService` - Azure Table Storage operations with cache invalidation
- **DI Registration**: `DependencyInjection.cs` - Extension method to wire up all services

**Key Pattern**: Infrastructure registers itself via `AddInfrastructure()` called from `Program.cs`.

#### Web Layer
**Location**: `src/Dashboard._Web/`

Presentation layer with MVC controllers, Razor views, and frontend assets.

**Contents**:
- **Controllers**: 5 feature controllers
  - `DashboardController` - Portfolio overview and analytics
  - `InvestmentController` - Investment breakdown by ticker
  - `MarketHistoryController` - Historical charts
  - `TransactionsController` - Transaction management
  - `SidebarController` - Navigation component
- **Views**: Razor templates
  - `Index.cshtml` - Main page layouts with skeleton loaders
  - `_*Content.cshtml` - Partial views loaded via AJAX
- **ViewModels**: View-specific data shapes (never cross layer boundaries)
- **wwwroot**: Static assets (CSS, JS, images, PWA manifest, service worker)

## Project Structure

```
src/
  Dashboard._Web/           # Presentation Layer
    Controllers/            # 5 MVC controllers (Dashboard, Investment, MarketHistory, Transactions, Sidebar)
    Views/                  # Razor views (Index.cshtml = pages, _*.cshtml = partials)
    ViewModels/             # View-specific data models (never used outside Web layer)
    wwwroot/
      css/                  # Compiled CSS from SCSS
      scss/                 # Modular SCSS (abstracts, base, layout, components, pages, vendor)
      js/                   # Client-side JavaScript
      manifest.json         # PWA manifest
      service-worker.js     # PWA service worker
      icon-*.png            # PWA icons

  Dashboard.Application/    # Business Logic Layer
    Dtos/                   # Data transfer objects (TransactionDto, MarketHistoryDto)
    Interfaces/             # Service contracts (ITickerApiService, IAzureTableService)
    Helpers/                # Static utilities (FilterHelper, FormattingHelper, PeriodHelper)
    Mappers/                # Entity ↔ DTO conversion (TransactionMapper)

  Dashboard.Domain/         # Core Domain Layer
    Entities/               # Domain models (TransactionEntity)
    Utils/                  # Constants (StaticDetails)

  Dashboard.Infrastructure/ # Infrastructure Layer
    Services/               # External service implementations (TickerApiService, AzureTableService)
    DependencyInjection.cs  # Service registration

tests/
  Dashboard.Tests/          # xUnit test suite
    Application/
      Helpers/              # Helper tests (FilterHelper, FormattingHelper, TransactionMapper)
```

### Key Directories

| Directory | Purpose |
|---|---|
| `src/Dashboard._Web/Controllers/` | MVC controllers for each feature area |
| `src/Dashboard._Web/Views/` | Razor views (Index = pages, _* = partials) |
| `src/Dashboard._Web/ViewModels/` | View-specific data shapes |
| `src/Dashboard.Application/Dtos/` | Cross-layer data transfer objects |
| `src/Dashboard.Application/Helpers/` | Reusable business logic utilities |
| `src/Dashboard.Application/Mappers/` | Entity ↔ DTO conversion |
| `src/Dashboard.Infrastructure/Services/` | External service implementations |
| `src/Dashboard._Web/wwwroot/scss/` | Modular SCSS architecture |
| `tests/Dashboard.Tests/` | xUnit tests with Arrange-Act-Assert pattern |

## Architectural Patterns

### Constructor Injection

All dependencies are injected via constructors. No service locator pattern except for the concurrent scope pattern (see below).

**Example** (`src/Dashboard._Web/Controllers/DashboardController.cs`):
```csharp
public class DashboardController : Controller
{
    private readonly IAzureTableService _azureTableService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardController> _logger;
    
    public DashboardController(
        IAzureTableService azureTableService,
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardController> logger)
    {
        _azureTableService = azureTableService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
}
```

### Concurrent Scope Pattern

When fetching market history for multiple tickers in parallel, scoped services cannot be shared across concurrent tasks. Solution:

1. Inject `IServiceScopeFactory` instead of the service directly
2. Create a new scope per concurrent task
3. Resolve the scoped service from the new scope

**Implementation** (`src/Dashboard._Web/Controllers/DashboardController.cs:143-161`):
```csharp
private async Task<MarketHistoryResponseDto?> GetMarketHistoryForTickerAsync(string ticker, string period)
{
    using var scope = _scopeFactory.CreateScope();
    var tickerApiService = scope.ServiceProvider.GetRequiredService<ITickerApiService>();
    return await tickerApiService.GetMarketHistoryResponseAsync(ticker, period, "1d");
}

// Called in parallel
var historyTasks = uniqueTickers.Select(ticker => 
    GetMarketHistoryForTickerAsync(ticker, period));
var histories = await Task.WhenAll(historyTasks);
```

### Cache-Aside with Invalidation

Both `TickerApiService` and `AzureTableService` follow the same caching pattern:

1. Check `IMemoryCache` for cached value
2. On cache miss: fetch from external source, cache with sliding (10min) + absolute (60min) expiration
3. On mutation (add/delete): explicitly invalidate cache entry

**Cache Keys**:
- Ticker API: `history:{ticker}:{period}:{interval}`
- Azure Table: `"transactions"`

**Configuration** (`src/Dashboard.Domain/Utils/StaticDetails.cs`):
```csharp
public static int CacheSlidingExpirationMinutes = 10;
public static int CacheAbsoluteExpirationMinutes = 60;
```

**Implementation** (`src/Dashboard.Infrastructure/Services/TickerApiService.cs:32-50`):
```csharp
var cacheKey = $"history:{ticker}:{period}:{interval}";
if (_cache.TryGetValue(cacheKey, out MarketHistoryResponseDto? cachedData))
{
    return cachedData;
}

var response = await FetchFromApiAsync(...);

_cache.Set(cacheKey, response, new MemoryCacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.CacheSlidingExpirationMinutes),
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.CacheAbsoluteExpirationMinutes)
});
```

**Cache Invalidation** (`src/Dashboard.Infrastructure/Services/AzureTableService.cs:60, 73`):
```csharp
// After adding transaction
_cache.Remove("transactions");

// After deleting transaction
_cache.Remove("transactions");
```

### Skeleton Loader + Partial View Pattern

All four main views use a two-phase loading pattern:

**Phase 1**: Controller returns a page with skeleton placeholders (immediate render)
```csharp
public IActionResult Index() => View();
```

**Phase 2**: Client-side JavaScript fetches content endpoint via AJAX
```javascript
fetch('/Dashboard/content')
    .then(response => response.text())
    .then(html => $('#dashboard-container').html(html));
```

**Phase 3**: Controller returns a partial view with real data
```csharp
public async Task<IActionResult> DashboardContent()
{
    var viewModel = await BuildViewModelAsync();
    return PartialView("_DashboardContent", viewModel);
}
```

**Benefits**:
- Immediate page render (perceived performance)
- Progressive enhancement
- Separation of layout and data loading

### DTO / ViewModel / Entity Separation

Three distinct model types with clear boundaries:

| Type | Layer | Purpose | Example |
|---|---|---|---|
| **Entity** | Domain | Azure Table Storage persistence | `TransactionEntity` |
| **DTO** | Application | Cross-layer data transfer | `TransactionDto`, `MarketHistoryResponseDto` |
| **ViewModel** | Web | View-specific shape | `DashboardViewModel`, `LineChartViewModel` |

**Conversion**: Entity ↔ DTO mapping via `TransactionMapper` (`src/Dashboard.Application/Mappers/TransactionMapper.cs`):
```csharp
public static TransactionEntity ToEntity(this TransactionDto dto)
public static TransactionDto ToModel(this TransactionEntity entity)
```

**Rule**: ViewModels never cross layer boundaries. DTOs are used for all inter-layer communication.

### Culture-Aware Data Handling

Strict boundary between storage and display:

- **Persistence**: Always `InvariantCulture` - decimals and dates stored as invariant strings
- **UI Boundary**: Locale-specific formatting applied only in views/ViewModels
- **Parsing**: `FormattingHelper.ParseDecimal()` tries InvariantCulture first, falls back to nl-NL
- **Date Format**: ISO 8601 (`yyyy-MM-dd`) for storage and API communication

**Implementation** (`src/Dashboard.Application/Helpers/FormattingHelper.cs`):
```csharp
public static decimal? ParseDecimal(string? value)
{
    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        return result;
    
    if (decimal.TryParse(value, NumberStyles.Any, new CultureInfo("nl-NL"), out result))
        return result;
    
    return null;
}
```

### Interface-Based Service Abstraction

External dependencies are abstracted behind interfaces defined in the Application layer:

- `ITickerApiService` - Market data fetching
- `IAzureTableService` - Transaction CRUD operations

**Benefits**:
- Testability (easy to mock)
- Loose coupling
- Dependency inversion (high-level modules don't depend on low-level implementations)

### Static Helper Pattern

Reusable business logic organized into static helper classes in `src/Dashboard.Application/Helpers/`:

- **FilterHelper** - Transaction filtering by tickers, date ranges, time ranges
- **FormattingHelper** - Decimal/date parsing and formatting with culture handling
- **PeriodHelper** - Time range to API period parameter conversion

**Characteristics**:
- Pure functions with no state
- No external dependencies
- Easily testable
- All test coverage focuses on these helpers

## SCSS Architecture

Design tokens are centralized in `src/Dashboard._Web/wwwroot/scss/abstracts/_tokens.scss` as CSS custom properties.

**Structure**:
```
scss/
  abstracts/    # Design tokens (colors, spacing, shadows, breakpoints)
    _tokens.scss
  base/         # Typography, responsive utilities, resets
  layout/       # Sidebar, bottom nav, base layout structure
  components/   # Cards, buttons, tables, charts, metrics, modals
  pages/        # Page-specific overrides (dashboard, transactions)
  vendor/       # Third-party overrides (DataTables, Flatpickr)
  main.scss     # Entry point - imports all partials in dependency order
```

**Principles**:
- Token-based design - all components reference tokens, not hard-coded values
- Modular organization - each component/page has its own file
- No global styles - everything scoped to specific components
- Bootstrap-first - leverage Bootstrap utilities, add custom styles only when needed

**Compilation**:
```bash
npm run sass:build   # Compile once
npm run sass:watch   # Watch mode
```

## Configuration

### Global Constants

All application-wide constants are centralized in `src/Dashboard.Domain/Utils/StaticDetails.cs`:

```csharp
public static class StaticDetails
{
    public const string UserId = "user1";
    public const string TransactionsTableName = "Transactions";
    public const int BufferDays = 7;
    
    public static int CacheSlidingExpirationMinutes = 10;
    public static int CacheAbsoluteExpirationMinutes = 60;
}
```

### Dependency Injection

All service registration happens in `src/Dashboard.Infrastructure/DependencyInjection.cs`:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Register TableClient
    services.AddSingleton(provider => {
        var connectionString = configuration["TRANSACTIONS_TABLE_CONNECTION_STRING"];
        return new TableClient(connectionString, StaticDetails.TransactionsTableName);
    });
    
    // Register services
    services.AddScoped<IAzureTableService, AzureTableService>();
    services.AddScoped<ITickerApiService, TickerApiService>();
    
    return services;
}
```

Called from `src/Dashboard._Web/Program.cs:28`:
```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

## Testing Strategy

Tests use xUnit with Arrange-Act-Assert pattern.

**Test Coverage** (`tests/Dashboard.Tests/Application/Helpers/`):
- `FilterHelperTests.cs` - Transaction and date range filtering
- `FormattingHelperTests.cs` - Decimal/date parsing and formatting
- `TransactionMapperTests.cs` - Entity ↔ DTO conversion
- `YearFilterIntegrationTests.cs` - Year-based filtering integration

**Philosophy**: Focus tests on business logic (helpers, mappers). Controllers and views are tested via integration/manual testing.

## Design Decisions

### Why Clean Architecture?
- Clear separation of concerns
- Testable business logic
- Easy to swap external dependencies (e.g., replace Azure Table Storage with SQL)
- Domain-centric design

### Why Memory Caching?
- Reduces API calls to external Ticker API
- Improves response times
- Simple to implement and maintain
- Appropriate for single-instance deployment

### Why Partial Views with AJAX?
- Immediate page render (better perceived performance)
- Skeleton loaders provide visual feedback
- Separates layout from data loading
- Progressive enhancement

### Why Static Helpers?
- Simple, testable, reusable
- No need for dependency injection overhead
- Pure functions with no side effects
- Easy to reason about and maintain

---

[← Back to Documentation Index](./README.md)
