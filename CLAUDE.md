# Portfolio Insight Dashboard

Investment portfolio tracking and visualization PWA built with .NET 10 (C# 12) and ASP.NET Core MVC. Displays real-time portfolio performance, market data charts, transaction management, and push notifications. Deployed to Azure Web App (`as-kvandijk-portfolio-dashboard`).

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core MVC, C# 12
- **Auth**: Azure AD via Microsoft.Identity.Web (OpenID Connect)
- **Storage**: Azure Table Storage (`Azure.Data.Tables`) — two tables: `transactions`, `pushsubscriptions`
- **Secrets**: Azure Key Vault (production); environment variables / user secrets (development)
- **Caching**: `IMemoryCache` (sliding 1min / absolute 5min)
- **Push Notifications**: Web Push API via `WebPush` NuGet package (VAPID)
- **Background Services**: `PortfolioMonitorService` — scheduled portfolio checks every 3h (08:00–20:00)
- **Frontend**: Bootstrap 5.3, Chart.js, DataTables, jQuery, SCSS (Sass)
- **Localization**: nl-NL (primary), en-US — cookie-based culture switching
- **CI/CD**: GitHub Actions (.NET 10, Ubuntu runners)

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

## Build & Run Commands

```bash
# Development (concurrent SCSS watch + dotnet hot reload)
npm run dev

# SCSS only
npm run sass:build          # compile once
npm run sass:watch          # watch mode

# .NET
dotnet restore              # restore NuGet packages
dotnet build                # build solution
dotnet test                 # run all xUnit tests
dotnet build --configuration Release --no-restore   # CI build

# Run application
dotnet watch run --project src/Dashboard._Web/Dashboard._Web.csproj
```

## Environment Variables / Configuration

In production, secrets are pulled from **Azure Key Vault** (`KeyVaultUri` in `appsettings.json`). In development, use `dotnet user-secrets` or environment variables.

| Variable / Key | Purpose |
|---|---|
| `ConnectionStrings__StorageAccount` | Azure Table Storage connection string (both tables) |
| `TICKER_API_URL` | Base URL for market data API |
| `TICKER_API_CODE` | API authentication key |
| `AzureAd__TenantId` | Azure AD tenant ID |
| `AzureAd__ClientId` | Azure AD client ID |
| `AzureAd__ClientSecret` | Azure AD client secret |
| `vapid-public-key` | VAPID public key for Web Push |
| `vapid-private-key` | VAPID private key for Web Push |
| `vapid-subject` | VAPID subject (mailto: or URL) |
| `KeyVaultUri` | Azure Key Vault URI (non-dev only) |

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

See `.claude/docs/architectural_patterns.md` for full details. Key patterns in brief:

**Clean Architecture**: Domain ← Application ← {Web, Infrastructure}. Domain has zero external dependencies.

**Skeleton loader + partial view**: `Index()` renders immediately with skeleton placeholders; client JS fetches `/{controller}/content` → `*Content()` returns a `PartialView`. Applied across Dashboard, Investment, MarketHistory, Transactions.

**Concurrent scope pattern**: When fetching market data for multiple tickers in parallel, controllers/services inject `IServiceScopeFactory` and create a new DI scope per task, then use `Task.WhenAll`. See `DashboardController.GetMarketHistoryForTickerAsync()` and `PortfolioValueService.GetTopHoldingsByValueAsync()`.

**Cache-aside**: Check `IMemoryCache` → on miss fetch + cache; invalidate on mutation. Cache keys: `history:{ticker}:{period}:{interval}` (TickerApiService), `"transactions"` (TransactionService).

**Culture-aware data**: Persistence always uses InvariantCulture. Locale formatting is applied only in views. `FormattingHelper.ParseDecimal()` tries InvariantCulture first, falls back to nl-NL. Dates stored as ISO 8601 (`yyyy-MM-dd`).

**Web Push Notifications**: `PortfolioMonitorService` (BackgroundService) runs every 3h within the 08:00–20:00 window. It fetches top 3 holdings via `IPortfolioValueService`, builds a notification, then sends to all subscribers via `IPushNotificationService` (VAPID). Expired subscriptions (HTTP 410 Gone) are auto-removed.

## Additional Documentation

| Document | When to check |
|---|---|
| `.claude/docs/architectural_patterns.md` | When making structural changes, adding services, or modifying data flow |
| `README.md` | For Ticker API reference, PWA details, setup instructions, and environment config |
