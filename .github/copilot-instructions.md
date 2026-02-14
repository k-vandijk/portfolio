# Copilot Instructions for Portfolio Insight Dashboard

This file provides guidance for GitHub Copilot when working on the Portfolio Insight Dashboard project.

## Project Overview

**Portfolio Insight Dashboard** is a .NET 10 investment portfolio tracking and visualization PWA built with ASP.NET Core MVC, Azure Table Storage, and a custom Ticker API. The application displays real-time portfolio performance, market data charts, and transaction management.

**Deployment**: Azure Web App with automated CI/CD via GitHub Actions.

## Documentation Navigation

### Entry Point
Start with **[README.md](../README.md)** for project overview and quick start.

### Detailed Documentation
Navigate to **[docs/README.md](../docs/README.md)** for the documentation index, then:

- **[docs/setup.md](../docs/setup.md)** - Installation, prerequisites, environment configuration
- **[docs/architecture.md](../docs/architecture.md)** - Clean Architecture, patterns, project structure
- **[docs/development.md](../docs/development.md)** - Development workflows, build commands, SCSS, testing
- **[docs/conventions.md](../docs/conventions.md)** - Naming conventions, coding standards, best practices
- **[docs/api-reference.md](../docs/api-reference.md)** - Ticker API endpoints and integration
- **[docs/pwa.md](../docs/pwa.md)** - Progressive Web App features and configuration
- **[docs/ci-cd.md](../docs/ci-cd.md)** - CI/CD workflows and deployment process

## Project-Specific Conventions

### Language

- **Code**: Always English (identifiers, comments, documentation)
- **Team Communication**: Primarily Dutch, English when context requires
- **Commit Messages**: English

### Naming Conventions

| Type | Convention | Example |
|---|---|---|
| Controllers | `*Controller` | `DashboardController` |
| ViewModels | `*ViewModel` | `DashboardViewModel` |
| DTOs | `*Dto` | `TransactionDto` |
| Entities | `*Entity` | `TransactionEntity` |
| Interfaces | `I*` prefix | `ITickerApiService` |
| Helpers | `*Helper` (static) | `FilterHelper`, `FormattingHelper` |
| Services | `*Service` | `TickerApiService` |
| Views (main) | `Index.cshtml` | `Views/Dashboard/Index.cshtml` |
| Views (partial) | `_*.cshtml` | `_DashboardContent.cshtml` |

### Architecture Boundaries

**Clean Architecture** - Dependencies flow inward:
```
Web → Application → Domain
    → Infrastructure → Domain
```

**Layer Rules**:
- Domain has zero dependencies
- Application defines interfaces; Infrastructure implements
- ViewModels never cross layer boundaries
- DTOs used for all cross-layer communication

**Key Files**:
- `src/Dashboard.Domain/Utils/StaticDetails.cs` - Global constants
- `src/Dashboard.Infrastructure/DependencyInjection.cs` - Service registration
- `src/Dashboard._Web/Program.cs` - Application bootstrap

## Guardrails & Best Practices

### Don't Guess - Cite Paths

When referencing code, always provide full file paths:
```
✅ Good: "Update FormattingHelper.ParseDecimal() in src/Dashboard.Application/Helpers/FormattingHelper.cs:15"
❌ Bad: "Update the ParseDecimal method"
```

### Ask Before Large Refactors

Before making structural changes:
1. Explain the proposed change and why
2. List affected files and components
3. Wait for user confirmation

**Examples requiring confirmation**:
- Changing layer boundaries
- Modifying dependency injection setup
- Restructuring database schema
- Changing authentication flow

### Keep Changes Small

Prefer surgical, minimal changes:
```
✅ Good: Modify one method to fix bug
❌ Bad: Refactor entire controller to fix bug
```

**Exception**: When refactoring is explicitly requested.

### Prefer Tests

When modifying business logic:
1. Check if tests exist (`tests/Dashboard.Tests/Application/Helpers/`)
2. Run existing tests: `dotnet test`
3. Add tests for new logic
4. Verify all tests pass

**Test Pattern**: Arrange-Act-Assert (see `docs/conventions.md`)

### DRY Principle

**If used 2+ times** → Create reusable component/helper/service

**Example**:
```csharp
// ✅ Good: Reusable helper
public static class FormattingHelper
{
    public static string FormatCurrency(decimal value, string currency)
    {
        return $"{value:N2} {currency}";
    }
}

// ❌ Bad: Duplicated logic
var formattedValue = $"{value:N2} USD"; // Repeated across controllers
```

**If logic is similar** → Refactor to shared implementation

### Bootstrap-First UI Development

Always prefer Bootstrap utilities over custom CSS:
```html
<!-- ✅ Good: Bootstrap utilities -->
<div class="d-flex justify-content-between align-items-center mb-3">
  <h1 class="h3">Dashboard</h1>
</div>

<!-- ❌ Bad: Custom CSS for common layouts -->
<div class="custom-header">
  <h1>Dashboard</h1>
</div>
```

**Only add custom SCSS when Bootstrap doesn't provide the functionality**.

### SCSS Guidelines

When adding custom styles:

1. **Use design tokens** - Reference variables from `src/Dashboard._Web/wwwroot/scss/abstracts/_tokens.scss`
```scss
// ✅ Good
.card {
  background-color: var(--color-surface);
  padding: var(--spacing-md);
}

// ❌ Bad
.card {
  background-color: #161B22;
  padding: 16px;
}
```

2. **Organize by concern**:
   - Component-specific → `components/_component-name.scss`
   - Page-specific → `pages/_page-name.scss`
   - Layout → `layout/_layout-element.scss`

3. **Compile after changes**: `npm run sass:build`

### Configuration Constants

Never hard-code values that appear in `src/Dashboard.Domain/Utils/StaticDetails.cs`:
```csharp
// ✅ Good: Use constants
var cacheMinutes = StaticDetails.CacheSlidingExpirationMinutes;
var tableName = StaticDetails.TransactionsTableName;

// ❌ Bad: Hard-coded values
var cacheMinutes = 10;
var tableName = "Transactions";
```

## Common Patterns

### Caching Pattern

```csharp
var cacheKey = $"entity:{identifier}:{parameters}";

if (_cache.TryGetValue(cacheKey, out TData? cachedData))
{
    return cachedData;
}

var data = await FetchDataAsync();

_cache.Set(cacheKey, data, new MemoryCacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.CacheSlidingExpirationMinutes),
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.CacheAbsoluteExpirationMinutes)
});

return data;
```

### Skeleton Loader + Partial View Pattern

All main views use this pattern:

**Controller**:
```csharp
public IActionResult Index() => View(); // Returns skeleton immediately

public async Task<IActionResult> Content()
{
    var viewModel = await BuildViewModelAsync();
    return PartialView("_Content", viewModel);
}
```

**Client-side**:
```javascript
fetch('/Controller/Content')
    .then(response => response.text())
    .then(html => $('#container').html(html));
```

### Concurrent API Calls

When fetching multiple tickers in parallel, use the **concurrent scope pattern**:

```csharp
private readonly IServiceScopeFactory _scopeFactory;

private async Task<MarketHistoryResponseDto?> GetMarketHistoryForTickerAsync(string ticker, string period)
{
    using var scope = _scopeFactory.CreateScope();
    var tickerApiService = scope.ServiceProvider.GetRequiredService<ITickerApiService>();
    return await tickerApiService.GetMarketHistoryResponseAsync(ticker, period, "1d");
}

// Call in parallel
var historyTasks = tickers.Select(t => GetMarketHistoryForTickerAsync(t, period));
var histories = await Task.WhenAll(historyTasks);
```

**Why**: Scoped services can't be shared across parallel tasks.

### Entity ↔ DTO Mapping

Always use `TransactionMapper` extension methods:

```csharp
// Entity → DTO
var dto = entity.ToModel();

// DTO → Entity
var entity = dto.ToEntity();
```

**Location**: `src/Dashboard.Application/Mappers/TransactionMapper.cs`

## Definition of Done

A task is complete when:

✅ **Functionality**
- Feature works as specified
- No compiler warnings
- Follows naming conventions
- Uses existing patterns (caching, partial views, etc.)

✅ **Testing**
- Unit tests added for new business logic (helpers, mappers)
- All tests pass: `dotnet test`
- No test coverage regression

✅ **Code Quality**
- No hard-coded values (use constants/config)
- No duplicate code (DRY principle)
- Bootstrap-first UI (minimal custom CSS)
- Design tokens used for custom styles
- Clean Architecture boundaries respected

✅ **Documentation**
- XML comments for public APIs (if applicable)
- README/docs updated if new features added
- Architecture docs updated if structure changed

✅ **Security**
- No secrets in code
- Input validation where needed
- Proper authentication/authorization

✅ **Build & Deploy**
- `dotnet build --configuration Release` succeeds
- `npm run sass:build` succeeds
- No merge conflicts with `main`

## Preferred Commands

### Development

```bash
# Start development server (SCSS watch + .NET hot reload)
npm run dev

# Build frontend assets
npm run sass:build

# Run tests
dotnet test

# Build solution
dotnet build
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~FilterHelperTests"
```

### Debugging

```bash
# Hot reload (recommended)
npm run dev

# .NET watch only
dotnet watch run --project src/Dashboard._Web/Dashboard._Web.csproj
```

## Working with Existing Context

This project previously used CLAUDE.md as a terse technical reference. Key points from that context:

### Tech Stack Summary
- **Backend**: .NET 10, ASP.NET Core MVC, C# 12
- **Auth**: Azure AD (OpenID Connect)
- **Storage**: Azure Table Storage
- **Caching**: `IMemoryCache`
- **Frontend**: Bootstrap 5.3, Chart.js, DataTables, jQuery, SCSS
- **CI/CD**: GitHub Actions

### Key Configuration Files
- `Directory.Build.props` - Shared MSBuild properties
- `Directory.Packages.props` - Centralized package versions
- `package.json` - npm scripts and frontend dependencies
- `src/Dashboard._Web/appsettings.json` - Application configuration

### Important Notes
- Nullable reference types enabled; warnings treated as errors
- Culture-aware: nl-NL (primary), en-US (secondary)
- All dates stored as ISO 8601 (`yyyy-MM-dd`)
- All decimals stored in InvariantCulture

## Anti-Patterns to Avoid

### ❌ Service Locator Pattern

```csharp
// ❌ Bad: Service locator (except concurrent scope pattern)
var service = serviceProvider.GetService<IMyService>();
```

```csharp
// ✅ Good: Constructor injection
public class MyController : Controller
{
    private readonly IMyService _myService;
    
    public MyController(IMyService myService)
    {
        _myService = myService;
    }
}
```

### ❌ ViewModels Crossing Boundaries

```csharp
// ❌ Bad: Passing ViewModel to Application layer
public class TransactionService
{
    public void Process(DashboardViewModel viewModel) { } // Wrong layer
}
```

```csharp
// ✅ Good: Use DTOs for cross-layer communication
public class TransactionService
{
    public void Process(TransactionDto dto) { } // Correct
}
```

### ❌ Hard-Coded Configuration

```csharp
// ❌ Bad
var cacheMinutes = 10;
var tableName = "Transactions";
```

```csharp
// ✅ Good
var cacheMinutes = StaticDetails.CacheSlidingExpirationMinutes;
var tableName = StaticDetails.TransactionsTableName;
```

## Questions to Ask

When unclear, ask:

- "Which layer should this logic live in?" (refer to `docs/architecture.md`)
- "Does similar logic already exist?" (avoid duplication)
- "Should this be tested?" (if business logic, yes)
- "Is this a breaking change?" (affects existing functionality)
- "Should this be configurable?" (environment-specific values)

## Resources

- **Main README**: [README.md](../README.md)
- **Docs Index**: [docs/README.md](../docs/README.md)
- **Architecture Guide**: [docs/architecture.md](../docs/architecture.md)
- **Conventions**: [docs/conventions.md](../docs/conventions.md)
- **Development Guide**: [docs/development.md](../docs/development.md)
- **Repository**: [github.com/k-vandijk/portfolio-insight-dashboard](https://github.com/k-vandijk/portfolio-insight-dashboard)

---

**Remember**: When in doubt, check the docs first, then ask clarifying questions before implementing.
