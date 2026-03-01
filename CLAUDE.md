# Portfolio

Investment portfolio tracking and visualization PWA built with .NET 10 (C# 12) and ASP.NET Core MVC. Displays real-time portfolio performance, market data charts, transaction management, and push notifications. Deployed to Azure Web App (`as-kvandijk-portfolio-dashboard`).

See `README.md` for tech stack, setup, environment variables, API reference, and CI/CD.

## Project Structure

```
src/
  Dashboard._Web/          # Presentation: Controllers, Views, ViewModels, static assets
  Dashboard.Application/   # Business logic: DTOs, Interfaces, Helpers, Mappers
  Dashboard.Domain/        # Core: models, StaticDetails constants, DashboardPresentationModes
  Dashboard.Infrastructure/ # External services: all service implementations, DI, BackgroundService
tests/
  Dashboard.Tests/         # xUnit tests for helpers and mappers
```

### Key Directories

| Directory | Purpose |
|---|---|
| `src/Dashboard._Web/Controllers/` | 6 controllers: Dashboard, Investment, MarketHistory, Transactions, Sidebar, Notifications |
| `src/Dashboard._Web/Views/` | Razor views — `Index.cshtml` for pages, `_*.cshtml` for partials |
| `src/Dashboard._Web/ViewModels/` | View-specific data shapes (never used outside Web layer) |
| `src/Dashboard.Application/Dtos/` | DTOs between layers: TransactionDto, MarketHistoryResponseDto, DataPointDto, HoldingInfoDto, PushSubscriptionDto |
| `src/Dashboard.Application/Interfaces/` | Service contracts: ITransactionService, ITickerApiService, IPushSubscriptionService, IPushNotificationService, IPortfolioValueService |
| `src/Dashboard.Application/Helpers/` | FilterHelper, FormattingHelper, PeriodHelper (static, pure functions) |
| `src/Dashboard.Application/Mappers/` | TransactionMapper, PushSubscriptionMapper (Entity ↔ DTO extension methods) |
| `src/Dashboard.Infrastructure/Services/` | TransactionService, TickerApiService, PushSubscriptionService, PushNotificationService, PortfolioValueService, PortfolioMonitorService |
| `src/Dashboard.Domain/Models/` | TransactionEntity, PushSubscriptionEntity (Azure Table Storage ITableEntity) |
| `src/Dashboard.Domain/Utils/` | StaticDetails (constants), DashboardPresentationModes (view mode constants) |
| `src/Dashboard._Web/wwwroot/scss/` | Modular SCSS: abstracts, base, layout, components, pages, vendor |
| `src/Dashboard._Web/wwwroot/js/` | site.js, skeleton.js, alerts.js, push-notifications.js |

## Commands

```bash
npm run dev                                                          # SCSS watch + dotnet hot reload (concurrent)
npm run sass:watch                                                   # SCSS watch only
dotnet watch run --project src/Dashboard._Web/Dashboard._Web.csproj # .NET hot reload only
dotnet test                                                          # run all xUnit tests
dotnet build --configuration Release --no-restore                    # CI release build
```

> Config keys use `__` separator in code (e.g., `AzureAd__ClientId`). Azure Key Vault maps these to `--` in secret names (e.g., `AzureAd--ClientId`). See README for the full secret reference.

## Naming Conventions

- Controllers: `*Controller` — ViewModels: `*ViewModel` — DTOs: `*Dto`
- Interfaces: `I*` prefix — Helpers: `*Helper` (static utility classes)
- Views: `Index.cshtml` (pages), `_*.cshtml` (partials)
- Nullable reference types enabled; warnings treated as errors
- All code (identifiers, comments, strings) is English

## Testing

Tests use xUnit with Arrange-Act-Assert pattern. Test files live in `tests/Dashboard.Tests/Application/Helpers/`:
- `FilterHelperTests.cs` — transaction and date range filtering
- `FormattingHelperTests.cs` — decimal/date parsing and formatting
- `TransactionMapperTests.cs` — Entity ↔ DTO conversion
- `YearFilterIntegrationTests.cs` — year-based filtering integration

Add or update tests when modifying non-trivial business logic in Helpers or Mappers.

## Key Configuration

Global constants in `src/Dashboard.Domain/Utils/StaticDetails.cs` — cache durations (absolute 5min, sliding 1min), table names, partition keys, first transaction date, portfolio check interval (3h), notification window (08:00–20:00), API buffer days (7).

Dashboard view mode constants in `src/Dashboard.Domain/Utils/DashboardPresentationModes.cs` — `value`, `profit`, `profit-percentage`.

DI registration in `src/Dashboard.Infrastructure/DependencyInjection.cs` — extension method `AddInfrastructure()` registers:
- Two keyed `TableClient` singletons (`transactions`, `pushsubscriptions`)
- `ITransactionService → TransactionService` (scoped)
- `ITickerApiService → TickerApiService` (scoped)
- `IPushSubscriptionService → PushSubscriptionService` (scoped)
- `IPushNotificationService → PushNotificationService` (scoped)
- `IPortfolioValueService → PortfolioValueService` (scoped)
- `PortfolioMonitorService` as hosted background service

App bootstrap in `src/Dashboard._Web/Program.cs` — Azure Key Vault config loading (non-dev), Azure AD auth, localization (nl-NL default, cookie provider), middleware pipeline, default route `{controller=Dashboard}/{action=Index}/{id?}`.

## Architectural Patterns

See `.claude/docs/architectural_patterns.md` for full detail.

- **Clean Architecture**: Domain ← Application ← {Web, Infrastructure}. Domain has zero external dependencies.
- **Skeleton loader + partial view**: `Index()` serves skeleton immediately; JS fetches `/{controller}/content` → `*Content()` returns a `PartialView`. Used in Dashboard, Investment, MarketHistory, Transactions.
- **Concurrent scope**: `IServiceScopeFactory` + one DI scope per task + `Task.WhenAll` for parallel ticker fetches.
- **Cache-aside**: Check `IMemoryCache` → miss → fetch + cache; invalidate on mutation. Keys: `history:{ticker}:{period}:{interval}`, `"transactions"`.
- **Culture-aware data**: InvariantCulture for persistence; locale formatting in views only. `FormattingHelper.ParseDecimal()` falls back to nl-NL.
- **Web Push**: `PortfolioMonitorService` runs every 3h (08:00–20:00). Fetches top 3 holdings → VAPID push to all subscribers. HTTP 410 → auto-remove expired subscription.

## Additional Documentation

| Document | When to check |
|---|---|
| `.claude/docs/architectural_patterns.md` | When making structural changes, adding services, or modifying data flow |
| `README.md` | Tech stack, setup, environment variables, API reference, CI/CD |
