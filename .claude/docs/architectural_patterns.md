# Architectural Patterns

## Clean Architecture (Layered Dependencies)

The solution follows Clean Architecture with four projects. Dependencies flow inward:

```
Web (Presentation) -> Application (Business Logic) -> Domain (Core Models)
                   -> Infrastructure (External Services) -> Domain
```

- **Domain** has zero dependencies — pure models and constants
- **Application** defines interfaces that Infrastructure implements
- **Infrastructure** registers itself via `DependencyInjection.AddInfrastructure()` (`src/Dashboard.Infrastructure/DependencyInjection.cs:11`)
- **Web** only references Application and calls `AddInfrastructure()` in `Program.cs:28`

## Constructor Injection

All controllers and services receive dependencies via constructor injection. No service locator usage except for the concurrent scope pattern (see below).

Examples:
- `DashboardController` injects `IAzureTableService`, `IServiceScopeFactory`, `ILogger<T>`, `IStringLocalizer<T>` (`src/Dashboard._Web/Controllers/DashboardController.cs:14-17`)
- `TickerApiService` injects `IHttpClientFactory`, `IMemoryCache` (`src/Dashboard.Infrastructure/Services/TickerApiService.cs:15`)
- `AzureTableService` injects `TableClient`, `IMemoryCache` (`src/Dashboard.Infrastructure/Services/AzureTableService.cs:19`)

## Concurrent Scope Pattern for Parallel API Calls

When fetching market history for multiple tickers concurrently, scoped services can't be shared across parallel tasks. The pattern:

1. Inject `IServiceScopeFactory` instead of the service directly
2. Create a new scope per concurrent task
3. Resolve the scoped service from the new scope

See `DashboardController.GetMarketHistoryForTickerAsync()` (`src/Dashboard._Web/Controllers/DashboardController.cs:143-161`) — creates an isolated scope per ticker, resolves `ITickerApiService`, then fetches in parallel via `Task.WhenAll` at line 117.

## Cache-Aside with Invalidation on Mutation

Both services follow identical caching:
1. Check `IMemoryCache` for cached value
2. On miss: fetch from external source, cache with sliding (10min) + absolute (60min) expiration
3. On mutation (add/delete): explicitly remove cache entry

Applied in:
- `TickerApiService.GetMarketHistoryResponseAsync()` (`src/Dashboard.Infrastructure/Services/TickerApiService.cs:32-50`) — cache key: `history:{ticker}:{period}:{interval}`
- `AzureTableService.GetTransactionsAsync()` (`src/Dashboard.Infrastructure/Services/AzureTableService.cs:27-46`) — cache key: `"transactions"`
- Cache invalidation on writes: `AzureTableService.cs:60` and `AzureTableService.cs:73`

Cache durations are centralized in `StaticDetails` (`src/Dashboard.Domain/Utils/StaticDetails.cs:10-11`).

## Skeleton Loader + Partial View Pattern

Pages use a two-phase loading pattern across all four main views:

1. `Index()` returns a page with skeleton placeholders (immediate render)
2. Client-side JS fetches `/{controller}/content` endpoint via AJAX
3. `*Content()` action returns a `PartialView("_*Content", viewModel)` with real data
4. JS replaces skeleton with the partial view HTML

This pattern appears in:
- `DashboardController`: `Index()` at line 28, `DashboardContent()` at line 30 (`src/Dashboard._Web/Controllers/DashboardController.cs`)
- Same pattern in `InvestmentController`, `MarketHistoryController`, `TransactionsController`

## DTO / ViewModel / Entity Separation

Three distinct model types with clear boundaries:

| Type | Layer | Purpose | Example |
|---|---|---|---|
| Entity | Domain | Azure Table Storage persistence | `TransactionEntity` |
| DTO | Application | Cross-layer data transfer | `TransactionDto`, `MarketHistoryResponseDto` |
| ViewModel | Web | View-specific shape | `DashboardViewModel`, `LineChartViewModel` |

Conversion between Entity and DTO happens via `TransactionMapper` (`src/Dashboard.Application/Mappers/TransactionMapper.cs`) using extension methods `ToEntity()` and `ToModel()`.

## Culture-Aware Data Handling

Data formatting follows a strict boundary pattern:

- **Persistence**: Always InvariantCulture — decimals and dates stored as invariant strings in Azure Table Storage
- **UI boundary**: Locale-specific formatting applied only in views/ViewModels
- **Parsing**: `FormattingHelper.ParseDecimal()` tries InvariantCulture first, falls back to nl-NL (`src/Dashboard.Application/Helpers/FormattingHelper.cs`)
- **Dates**: ISO 8601 (`yyyy-MM-dd`) for storage and API communication

## Interface-Based Service Abstraction

External dependencies are abstracted behind interfaces defined in the Application layer:

- `ITickerApiService` — market data fetching (`src/Dashboard.Application/Interfaces/ITickerApiService.cs`)
- `IAzureTableService` — transaction CRUD (`src/Dashboard.Application/Interfaces/IAzureTableService.cs`)

Infrastructure implements these interfaces. The Web layer depends only on the interfaces, never on concrete service classes.

## Static Helper Pattern

Reusable logic is organized into static helper classes in `src/Dashboard.Application/Helpers/`:

- `FilterHelper` — transaction filtering by tickers, date ranges, time ranges
- `FormattingHelper` — decimal/date parsing and formatting with culture handling
- `PeriodHelper` — time range to API period parameter conversion

These are pure functions with no state or dependencies, making them easily testable. All test coverage focuses on these helpers (`tests/Dashboard.Tests/Application/Helpers/`).

## SCSS Token System

Design tokens are centralized in `src/Dashboard._Web/wwwroot/scss/abstracts/_tokens.scss` as CSS custom properties. All components reference tokens rather than hard-coded values.

SCSS is organized by concern:
```
scss/
  abstracts/   # Design tokens (colors, spacing, shadows)
  base/        # Typography, responsive utilities
  layout/      # Sidebar, bottom nav, base layout
  components/  # Cards, buttons, tables, charts, metrics
  pages/       # Page-specific overrides
  vendor/      # Third-party overrides (DataTables)
```

Entry point: `main.scss` imports all partials in dependency order.
