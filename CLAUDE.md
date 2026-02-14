# Portfolio Insight Dashboard

Investment portfolio tracking and visualization PWA built with .NET 10 (C# 12) and ASP.NET Core MVC. Displays real-time portfolio performance, market data charts, and transaction management. Deployed to Azure Web App.

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core MVC, C# 12
- **Auth**: Azure AD via Microsoft.Identity.Web (OpenID Connect)
- **Storage**: Azure Table Storage (`Azure.Data.Tables`)
- **Caching**: `IMemoryCache` (sliding 10min / absolute 60min)
- **Frontend**: Bootstrap 5.3, Chart.js, DataTables, jQuery, SCSS (Sass)
- **Localization**: nl-NL (primary), en-US — cookie-based culture switching
- **CI/CD**: GitHub Actions (.NET 10, Ubuntu runners)

## Project Structure

```
src/
  Dashboard._Web/          # Presentation: Controllers, Views, ViewModels, static assets
  Dashboard.Application/   # Business logic: DTOs, Interfaces, Helpers, Mappers
  Dashboard.Domain/        # Core: TransactionEntity model, StaticDetails constants
  Dashboard.Infrastructure/ # External services: TickerApiService, AzureTableService, DI
tests/
  Dashboard.Tests/         # xUnit tests for helpers and mappers
```

### Key Directories

| Directory | Purpose |
|---|---|
| `src/Dashboard._Web/Controllers/` | 5 controllers: Dashboard, Investment, MarketHistory, Transactions, Sidebar |
| `src/Dashboard._Web/Views/` | Razor views — `Index.cshtml` for pages, `_*.cshtml` for partials |
| `src/Dashboard._Web/ViewModels/` | View-specific data shapes (never used outside Web layer) |
| `src/Dashboard.Application/Dtos/` | Data transfer objects between layers |
| `src/Dashboard.Application/Helpers/` | FilterHelper, FormattingHelper, PeriodHelper |
| `src/Dashboard.Application/Mappers/` | TransactionMapper (Entity <-> DTO) |
| `src/Dashboard.Infrastructure/Services/` | TickerApiService, AzureTableService |
| `src/Dashboard._Web/wwwroot/scss/` | Modular SCSS: abstracts, base, layout, components, pages, vendor |

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

## Environment Variables

| Variable | Purpose |
|---|---|
| `TRANSACTIONS_TABLE_CONNECTION_STRING` | Azure Table Storage connection string |
| `TICKER_API_URL` | Base URL for market data API |
| `TICKER_API_CODE` | API authentication key |
| `AzureAd__TenantId` | Azure AD tenant ID |
| `AzureAd__ClientId` | Azure AD client ID |
| `AzureAd__ClientSecret` | Azure AD client secret |

## Naming Conventions

- Controllers: `*Controller` — ViewModels: `*ViewModel` — DTOs: `*Dto`
- Interfaces: `I*` prefix — Helpers: `*Helper` (static utility classes)
- Views: `Index.cshtml` (pages), `_*.cshtml` (partials)
- Nullable reference types enabled; warnings treated as errors

## Testing

Tests use xUnit with Arrange-Act-Assert pattern. Test files live in `tests/Dashboard.Tests/Application/Helpers/`:
- `FilterHelperTests.cs` — transaction and date range filtering
- `FormattingHelperTests.cs` — decimal/date parsing and formatting
- `TransactionMapperTests.cs` — Entity <-> DTO conversion
- `YearFilterIntegrationTests.cs` — year-based filtering integration

## Key Configuration

Global constants in `src/Dashboard.Domain/Utils/StaticDetails.cs:1-16` — cache durations, table names, partition key, API buffer days.

DI registration in `src/Dashboard.Infrastructure/DependencyInjection.cs:9-27` — extension method `AddInfrastructure()` wires up `TableClient`, `AzureTableService`, and `TickerApiService`.

App bootstrap in `src/Dashboard._Web/Program.cs:1-77` — auth, localization, middleware pipeline.

## Additional Documentation

Check these files for deeper guidance on specific topics:

| Document | When to check |
|---|---|
| `.claude/docs/architectural_patterns.md` | When making structural changes, adding services, or modifying data flow |
| `README.md` | For Ticker API reference, PWA details, setup instructions, and environment config |
