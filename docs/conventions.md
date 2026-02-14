# Conventions

This document outlines the coding standards, naming conventions, and best practices for the Portfolio Insight Dashboard project.

## Language & Communication

- **Code**: Always English (identifiers, comments, documentation)
- **Comments**: English only
- **Team Communication**: Primarily Dutch, English when context requires
- **Commit Messages**: English

## Naming Conventions

### C# Naming

#### Classes, Interfaces, and Types

| Type | Convention | Example |
|---|---|---|
| Controllers | `*Controller` suffix | `DashboardController` |
| ViewModels | `*ViewModel` suffix | `DashboardViewModel` |
| DTOs | `*Dto` suffix | `TransactionDto`, `MarketHistoryDto` |
| Entities | `*Entity` suffix | `TransactionEntity` |
| Interfaces | `I*` prefix | `ITickerApiService`, `IAzureTableService` |
| Helpers | `*Helper` suffix (static) | `FilterHelper`, `FormattingHelper` |
| Services | `*Service` suffix | `TickerApiService`, `AzureTableService` |

#### Methods and Properties

```csharp
// ✅ Good: PascalCase for public members
public async Task<TransactionDto> GetTransactionAsync(string id)
{
    // ...
}

public string TickerSymbol { get; set; }

// ✅ Good: camelCase for private fields with underscore prefix
private readonly ITickerApiService _tickerApiService;
private readonly ILogger<DashboardController> _logger;

// ✅ Good: camelCase for local variables
var transactionDto = new TransactionDto();
var marketHistory = await GetMarketHistoryAsync();
```

#### Constants and Static Fields

```csharp
// ✅ Good: PascalCase for public constants
public const string TransactionsTableName = "Transactions";
public const string UserId = "user1";

// ✅ Good: PascalCase for static fields
public static int CacheSlidingExpirationMinutes = 10;
```

### Views and Razor Files

| Type | Convention | Example |
|---|---|---|
| Main pages | `Index.cshtml` | `Views/Dashboard/Index.cshtml` |
| Partial views | `_*.cshtml` prefix | `_DashboardContent.cshtml` |
| Layout files | `_Layout.cshtml` | `Views/Shared/_Layout.cshtml` |
| View components | `*ViewComponent` | `SidebarViewComponent` |

### Frontend

#### JavaScript

```javascript
// ✅ Good: camelCase for variables and functions
const apiEndpoint = '/api/transactions';
function fetchTransactions() { }

// ✅ Good: PascalCase for classes
class TransactionManager { }

// ✅ Good: UPPER_SNAKE_CASE for constants
const API_BASE_URL = 'https://api.example.com';
```

#### CSS/SCSS

```scss
// ✅ Good: kebab-case for classes
.dashboard-container { }
.metric-card { }
.line-chart-wrapper { }

// ✅ Good: BEM methodology for components
.card { }
.card__title { }
.card__body { }
.card--highlighted { }

// ✅ Good: kebab-case for CSS custom properties
:root {
  --color-primary: #1560BD;
  --spacing-md: 1rem;
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.2);
}
```

## Project Organization

### Layer Boundaries

**Rule**: Dependencies flow inward toward the Domain layer.

```
Web → Application → Domain
     → Infrastructure → Domain
```

**Violations to Avoid**:
- ❌ Domain referencing Application, Infrastructure, or Web
- ❌ Application referencing Infrastructure or Web
- ❌ Infrastructure referencing Web

### Model Separation

| Model Type | Layer | Usage | Cross-Layer? |
|---|---|---|---|
| **Entity** | Domain | Persistence (Azure Table Storage) | ✅ Via DTO |
| **DTO** | Application | Cross-layer data transfer | ✅ Yes |
| **ViewModel** | Web | View-specific data shape | ❌ Never |

**Rules**:
- ViewModels NEVER cross layer boundaries
- DTOs are used for all inter-layer communication
- Entities are converted to DTOs via mappers

### File Organization

```
src/Dashboard.[Layer]/
  [Concept]/
    [File].cs
```

**Examples**:
- `src/Dashboard.Application/Helpers/FilterHelper.cs`
- `src/Dashboard.Infrastructure/Services/TickerApiService.cs`
- `src/Dashboard._Web/Controllers/DashboardController.cs`

## Code Style

### C# Style

#### Nullable Reference Types

**Enabled**: Warnings treated as errors

```csharp
// ✅ Good: Explicit nullability
public string? OptionalValue { get; set; }
public string RequiredValue { get; set; } = string.Empty;

// ✅ Good: Null-checking
if (value != null)
{
    ProcessValue(value);
}

// ✅ Good: Null-coalescing
var result = value ?? defaultValue;
```

#### Async/Await

```csharp
// ✅ Good: Async suffix for async methods
public async Task<TransactionDto> GetTransactionAsync(string id)
{
    return await _service.FetchAsync(id);
}

// ✅ Good: ConfigureAwait(false) in library code (not needed in ASP.NET Core)
// Note: Not used in this project (ASP.NET Core handles context properly)

// ❌ Bad: Avoid async void (except event handlers)
public async void ProcessData() // Don't do this
{
    await _service.FetchAsync();
}
```

#### Dependency Injection

```csharp
// ✅ Good: Constructor injection
public class DashboardController : Controller
{
    private readonly IAzureTableService _azureTableService;
    private readonly ILogger<DashboardController> _logger;
    
    public DashboardController(
        IAzureTableService azureTableService,
        ILogger<DashboardController> logger)
    {
        _azureTableService = azureTableService;
        _logger = logger;
    }
}

// ❌ Bad: Service locator pattern (except concurrent scope pattern)
var service = serviceProvider.GetService<IMyService>(); // Avoid
```

### SCSS Style

#### Bootstrap-First Development

```scss
// ✅ Good: Use Bootstrap utilities first
.dashboard-header {
  // Minimal custom styles, leverage Bootstrap classes in HTML
}

// ❌ Bad: Recreating Bootstrap functionality
.dashboard-header {
  display: flex;
  justify-content: space-between; // Use Bootstrap's .d-flex .justify-content-between instead
}
```

#### Token-Based Design

```scss
// ✅ Good: Reference design tokens
.card {
  background-color: var(--color-surface);
  padding: var(--spacing-md);
  box-shadow: var(--shadow-md);
}

// ❌ Bad: Hard-coded values
.card {
  background-color: #161B22;
  padding: 16px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.3);
}
```

#### Minimal Custom CSS

```scss
// ✅ Good: Minimal, specific custom styles
.metric-card {
  &__value {
    font-size: var(--font-size-xl);
    font-weight: 600;
  }
}

// ❌ Bad: Excessive custom styles that Bootstrap already provides
.metric-card {
  display: flex;
  flex-direction: column;
  border: 1px solid #ccc;
  border-radius: 4px;
  // ... lots of Bootstrap-duplicated styles
}
```

## DRY (Don't Repeat Yourself)

### When to Create Reusable Components

**If used 2+ times** → Create a reusable component/helper/service

#### Example: Partial Views

```html
<!-- ✅ Good: Reusable partial view -->
@await Html.PartialAsync("_MetricCard", new { 
    Title = "Total Value", 
    Value = totalValue 
})

<!-- ❌ Bad: Duplicated HTML across multiple views -->
<div class="metric-card">
  <h3>Total Value</h3>
  <p>@totalValue</p>
</div>
```

#### Example: Helper Methods

```csharp
// ✅ Good: Reusable helper
public static class FormattingHelper
{
    public static string FormatCurrency(decimal value, string currency)
    {
        return $"{value:N2} {currency}";
    }
}

// ❌ Bad: Duplicated logic in multiple controllers
var formattedValue = $"{value:N2} USD"; // Repeated everywhere
```

### When to Refactor Toward Shared Implementation

**If logic is similar** → Refactor to shared implementation

```csharp
// ✅ Good: Shared filtering logic
public static class FilterHelper
{
    public static IEnumerable<TransactionDto> FilterByTickers(
        IEnumerable<TransactionDto> transactions,
        IEnumerable<string>? tickers)
    {
        if (tickers == null || !tickers.Any())
            return transactions;
        
        return transactions.Where(t => tickers.Contains(t.Ticker));
    }
}

// ❌ Bad: Duplicated filtering in multiple controllers
var filtered = transactions.Where(t => selectedTickers.Contains(t.Ticker));
```

## Testing Standards

### Test Structure

**Pattern**: Arrange-Act-Assert

```csharp
[Fact]
public void FilterByTickers_ValidTickers_ReturnsFilteredTransactions()
{
    // Arrange
    var transactions = new List<TransactionDto>
    {
        new() { Ticker = "AAPL" },
        new() { Ticker = "GOOGL" },
        new() { Ticker = "MSFT" }
    };
    var tickers = new[] { "AAPL", "MSFT" };
    
    // Act
    var result = FilterHelper.FilterByTickers(transactions, tickers);
    
    // Assert
    Assert.Equal(2, result.Count());
    Assert.All(result, t => Assert.Contains(t.Ticker, tickers));
}
```

### Test Naming

**Convention**: `MethodName_Scenario_ExpectedResult`

```csharp
// ✅ Good test names
[Fact]
public void ParseDecimal_ValidInvariantCulture_ReturnsDecimal() { }

[Fact]
public void ParseDecimal_InvalidFormat_ReturnsNull() { }

[Fact]
public void FilterByDateRange_TransactionsOutsideRange_ExcludesTransactions() { }

// ❌ Bad test names
[Fact]
public void Test1() { }

[Fact]
public void TestParseDecimal() { }
```

## Definition of Done

A task is considered complete when:

✅ **Code Complete**
- Functionality implemented and working
- Follows naming conventions
- Passes all existing tests
- No compiler warnings

✅ **Testing**
- Unit tests added for new business logic (helpers, mappers)
- All tests pass locally
- Code coverage maintained or improved

✅ **Documentation**
- XML comments for public APIs (if applicable)
- README updated if new features added
- Architecture docs updated if structure changed

✅ **Code Review**
- Code reviewed (if working in a team)
- Feedback addressed
- No merge conflicts

✅ **Quality**
- No hard-coded values (use constants or configuration)
- No duplicate code (DRY principle)
- Follows Bootstrap-first UI approach
- Design tokens used for custom styles

✅ **Deployment Ready**
- Builds successfully in Release configuration
- No secrets in code (use environment variables)
- CI/CD pipeline passes

## Common Patterns

### Caching Pattern

```csharp
// Cache key format: "entity:{identifier}:{parameters}"
var cacheKey = $"history:{ticker}:{period}:{interval}";

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

### Partial View Loading Pattern

**Controller**:
```csharp
public IActionResult Index() => View(); // Skeleton

public async Task<IActionResult> Content()
{
    var viewModel = await BuildViewModelAsync();
    return PartialView("_Content", viewModel);
}
```

**JavaScript**:
```javascript
fetch('/Controller/Content')
    .then(response => response.text())
    .then(html => document.getElementById('container').innerHTML = html);
```

### Mapper Pattern

```csharp
public static class TransactionMapper
{
    public static TransactionEntity ToEntity(this TransactionDto dto)
    {
        return new TransactionEntity
        {
            PartitionKey = StaticDetails.UserId,
            RowKey = dto.Id,
            // ... map properties
        };
    }
    
    public static TransactionDto ToModel(this TransactionEntity entity)
    {
        return new TransactionDto
        {
            Id = entity.RowKey,
            // ... map properties
        };
    }
}
```

## Git Workflow

### Branch Naming

| Type | Convention | Example |
|---|---|---|
| Features | `feat-*` or `feature/*` | `feat-add-export-function` |
| Bug fixes | `fix-*` or `bugfix/*` | `fix-date-parsing-bug` |
| Hotfixes | `hotfix/*` | `hotfix/security-patch` |

### Commit Messages

**Format**: `<type>: <description>`

**Types**:
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting, no logic change)
- `refactor:` - Code refactoring
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks

**Examples**:
```
feat: add transaction export to CSV
fix: resolve date parsing for nl-NL locale
docs: update setup guide with Azure configuration
refactor: extract filtering logic to FilterHelper
test: add unit tests for FormattingHelper
```

## Security Guidelines

### Secrets Management

```csharp
// ✅ Good: Use configuration
var apiKey = _configuration["TICKER_API_CODE"];
var connectionString = _configuration["TRANSACTIONS_TABLE_CONNECTION_STRING"];

// ❌ Bad: Hard-coded secrets
var apiKey = "abc123xyz"; // Never do this
```

### Input Validation

```csharp
// ✅ Good: Validate user input
if (string.IsNullOrWhiteSpace(ticker) || ticker.Length > 10)
{
    return BadRequest("Invalid ticker");
}

// ✅ Good: Use model binding validation
public class TransactionDto
{
    [Required]
    [StringLength(10)]
    public string Ticker { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
}
```

## Performance Guidelines

### Database Queries

```csharp
// ✅ Good: Filter early
var filtered = transactions
    .Where(t => t.Date >= startDate)
    .Where(t => t.Ticker == ticker)
    .ToList();

// ❌ Bad: Filter after loading all data
var filtered = transactions.ToList()
    .Where(t => t.Date >= startDate && t.Ticker == ticker);
```

### Async Operations

```csharp
// ✅ Good: Parallel when possible
var tasks = tickers.Select(t => FetchDataAsync(t));
var results = await Task.WhenAll(tasks);

// ❌ Bad: Sequential when parallel is possible
var results = new List<Data>();
foreach (var ticker in tickers)
{
    results.Add(await FetchDataAsync(ticker)); // Slow
}
```

---

[← Back to Documentation Index](./README.md)
