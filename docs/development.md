# Development Guide

This guide covers development workflows, build commands, testing, and frontend development for the Portfolio Insight Dashboard.

## Development Workflow

### Quick Start

```bash
# Install dependencies
npm install
dotnet restore

# Start development server (SCSS watch + .NET hot reload)
npm run dev
```

The application runs at `https://localhost:5001` with automatic reload on code changes.

## Build Commands

### Frontend (SCSS)

| Command | Description |
|---|---|
| `npm run sass:build` | Compile SCSS → CSS once |
| `npm run sass:watch` | Watch SCSS files for changes (auto-compile) |

**SCSS Source**: `src/Dashboard._Web/wwwroot/scss/main.scss`  
**CSS Output**: `src/Dashboard._Web/wwwroot/css/site.css`

### Backend (.NET)

| Command | Description |
|---|---|
| `dotnet restore` | Restore NuGet packages |
| `dotnet build` | Build entire solution |
| `dotnet test` | Run all xUnit tests |
| `dotnet test --collect:"XPlat Code Coverage"` | Run tests with coverage report |
| `dotnet build --configuration Release --no-restore` | CI/production build |

### Combined Development Commands

| Command | Description |
|---|---|
| `npm run dev` | Concurrent SCSS watch + dotnet hot reload (recommended) |
| `npm run dotnet:watch` | .NET hot reload only |
| `dotnet watch run --project src/Dashboard._Web/Dashboard._Web.csproj` | Alternative .NET watch |

## Project Structure

For detailed architecture, see [Architecture Guide](./architecture.md).

**Quick Reference**:
```
src/
  Dashboard._Web/           # Presentation: Controllers, Views, ViewModels, wwwroot
  Dashboard.Application/    # Business logic: DTOs, Interfaces, Helpers, Mappers
  Dashboard.Domain/         # Core models: TransactionEntity, StaticDetails
  Dashboard.Infrastructure/ # External services: TickerApiService, AzureTableService

tests/
  Dashboard.Tests/          # xUnit tests
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~FilterHelperTests"
```

### Test Organization

Tests follow the **Arrange-Act-Assert** pattern and are located in `tests/Dashboard.Tests/Application/Helpers/`:

| Test File | Coverage |
|---|---|
| `FilterHelperTests.cs` | Transaction filtering, date range filtering, time range calculations |
| `FormattingHelperTests.cs` | Decimal parsing (InvariantCulture, nl-NL), date formatting |
| `TransactionMapperTests.cs` | Entity ↔ DTO conversion |
| `YearFilterIntegrationTests.cs` | Year-based filtering integration |

### Test Example

```csharp
[Fact]
public void ParseDecimal_ValidInvariantCulture_ReturnsDecimal()
{
    // Arrange
    var input = "1234.56";
    
    // Act
    var result = FormattingHelper.ParseDecimal(input);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(1234.56m, result.Value);
}
```

### Testing Philosophy

- **Unit tests** focus on business logic (helpers, mappers)
- **Controllers and views** tested via integration/manual testing
- **Pure functions** (static helpers) are easiest to test
- **Mock external dependencies** (Azure Table Storage, Ticker API) in integration tests

## SCSS Development

### Architecture

SCSS follows a modular architecture with centralized design tokens.

**Directory Structure**:
```
wwwroot/scss/
  abstracts/
    _tokens.scss      # Design tokens (colors, spacing, shadows, breakpoints)
  base/
    _typography.scss  # Font styles, headings
    _responsive.scss  # Responsive utilities
  layout/
    _sidebar.scss     # Desktop sidebar navigation
    _bottom-nav.scss  # Mobile bottom navigation
    _layout.scss      # Base layout structure
  components/
    _cards.scss       # Card components
    _buttons.scss     # Button styles
    _tables.scss      # Table styles
    _charts.scss      # Chart.js overrides
    _metrics.scss     # Metric display components
    _modals.scss      # Modal dialogs
  pages/
    _dashboard.scss   # Dashboard-specific styles
    _transactions.scss # Transactions page styles
  vendor/
    _datatables.scss  # DataTables overrides
    _flatpickr.scss   # Flatpickr date picker overrides
  main.scss           # Entry point (imports all partials)
```

### Design Tokens

All design tokens are defined as CSS custom properties in `abstracts/_tokens.scss`:

**Example**:
```scss
:root {
  // Colors
  --color-primary: #1560BD;
  --color-background: #0D1117;
  --color-surface: #161B22;
  
  // Spacing
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  
  // Shadows
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.2);
  --shadow-md: 0 4px 6px rgba(0, 0, 0, 0.3);
}
```

**Usage**:
```scss
.card {
  background-color: var(--color-surface);
  padding: var(--spacing-md);
  box-shadow: var(--shadow-md);
}
```

### Adding New Styles

**1. Determine the appropriate file**:
- **Component-specific** → `components/_component-name.scss`
- **Page-specific** → `pages/_page-name.scss`
- **Layout** → `layout/_layout-element.scss`
- **New design token** → `abstracts/_tokens.scss`

**2. Reference existing tokens**:
```scss
// ❌ Don't hard-code values
.my-component {
  background-color: #161B22;
  padding: 16px;
}

// ✅ Use design tokens
.my-component {
  background-color: var(--color-surface);
  padding: var(--spacing-md);
}
```

**3. Import in `main.scss`** (if new file):
```scss
@import 'components/my-component';
```

**4. Compile**:
```bash
npm run sass:build
```

### Bootstrap Integration

The project uses **Bootstrap 5.3.3** as the primary UI framework.

**Guidelines**:
- Prefer Bootstrap utilities over custom CSS
- Only add custom SCSS when Bootstrap doesn't provide the functionality
- Override Bootstrap variables in `abstracts/_tokens.scss` if needed

**Example**:
```html
<!-- ✅ Good: Use Bootstrap utilities -->
<div class="d-flex justify-content-between align-items-center mb-3">
  <h1 class="h3">Dashboard</h1>
</div>

<!-- ❌ Avoid: Custom CSS for common layouts -->
<div class="custom-header-container">
  <h1 class="custom-title">Dashboard</h1>
</div>
```

## Hot Reload & Live Development

### Using `npm run dev`

The recommended development workflow uses `concurrently` to run both SCSS watch and .NET hot reload:

```bash
npm run dev
```

This runs:
1. **SCSS Watch** - Automatically recompiles CSS on SCSS file changes
2. **.NET Hot Reload** - Automatically reloads the app on C# code changes

**Supported Changes** (hot reload without restart):
- Razor view updates
- Controller method changes
- Service implementation changes
- Static file changes (JS, CSS)

**Requires Restart**:
- `Program.cs` changes
- Dependency injection registration changes
- Environment variable changes

### Manual Watch Commands

**SCSS only**:
```bash
npm run sass:watch
```

**.NET only**:
```bash
dotnet watch run --project src/Dashboard._Web/Dashboard._Web.csproj
```

## Debugging

### Visual Studio

1. Open `ticker_api_dashboard.slnx`
2. Set `Dashboard._Web` as the startup project
3. Press **F5** to start debugging

### Visual Studio Code

1. Install the **C# Dev Kit** extension
2. Open the project folder
3. Press **F5** or use the Run panel
4. Set breakpoints in `.cs` files

### Browser DevTools

**Chrome DevTools**:
- **F12** or **Ctrl+Shift+I** - Open DevTools
- **Network tab** - Monitor AJAX calls to partial views
- **Console** - Check JavaScript errors
- **Application tab** - Inspect PWA manifest, service worker, cache storage

### Logging

The application uses **Serilog** for structured logging.

**Log Levels**:
- `Information` - Normal application flow
- `Warning` - Unexpected situations (e.g., cache misses)
- `Error` - Errors that don't stop the application (e.g., API failures)

**View Logs**:
- Console output during development
- Azure Application Insights in production

## Common Development Tasks

### Adding a New Controller Action

1. Add method to controller:
```csharp
public async Task<IActionResult> MyAction()
{
    var data = await _service.GetDataAsync();
    return View(data);
}
```

2. Create corresponding view: `Views/Controller/MyAction.cshtml`

3. Add route if needed (or use conventional routing)

### Adding a New Helper Method

1. Add static method to appropriate helper class in `src/Dashboard.Application/Helpers/`:
```csharp
public static class MyHelper
{
    public static string FormatValue(decimal value)
    {
        // Implementation
    }
}
```

2. Add unit tests in `tests/Dashboard.Tests/Application/Helpers/MyHelperTests.cs`

3. Run tests:
```bash
dotnet test
```

### Adding a New Service

1. Define interface in `src/Dashboard.Application/Interfaces/`:
```csharp
public interface IMyService
{
    Task<MyDto> GetDataAsync();
}
```

2. Implement in `src/Dashboard.Infrastructure/Services/`:
```csharp
public class MyService : IMyService
{
    public async Task<MyDto> GetDataAsync()
    {
        // Implementation
    }
}
```

3. Register in `src/Dashboard.Infrastructure/DependencyInjection.cs`:
```csharp
services.AddScoped<IMyService, MyService>();
```

### Adding a New SCSS Component

1. Create `src/Dashboard._Web/wwwroot/scss/components/_my-component.scss`

2. Add styles using design tokens:
```scss
.my-component {
  background-color: var(--color-surface);
  padding: var(--spacing-md);
  
  &__title {
    color: var(--color-text-primary);
    font-size: var(--font-size-lg);
  }
}
```

3. Import in `main.scss`:
```scss
@import 'components/my-component';
```

4. Compile:
```bash
npm run sass:build
```

## Performance Tips

### SCSS Compilation
- Use `sass:watch` during development for instant feedback
- Run `sass:build` before committing to ensure clean compilation
- Check compiled CSS size (should be under 100KB for good performance)

### .NET Build
- Use `dotnet build` incremental builds during development
- Use `dotnet build --no-incremental` if experiencing caching issues
- Clear `bin/` and `obj/` folders if builds behave unexpectedly

### Caching
- Check cache effectiveness by monitoring logs
- Adjust cache durations in `StaticDetails.cs` based on API usage
- Clear memory cache by restarting the application

## Troubleshooting

### SCSS Not Compiling

**Issue**: Changes to SCSS files not reflected in browser

**Solutions**:
1. Check `npm run sass:watch` is running
2. Hard refresh browser (Ctrl+F5)
3. Check for SCSS syntax errors in console
4. Verify import in `main.scss`

### Hot Reload Not Working

**Issue**: Code changes require manual restart

**Solutions**:
1. Ensure using `dotnet watch` or `npm run dev`
2. Check if change requires restart (e.g., `Program.cs`)
3. Restart watch command
4. Clear `bin/` and `obj/` folders

### Tests Failing

**Issue**: Tests pass locally but fail in CI

**Solutions**:
1. Ensure tests don't depend on local environment
2. Check for culture-specific formatting issues
3. Verify all dependencies are restored
4. Run `dotnet clean` before `dotnet test`

---

[← Back to Documentation Index](./README.md)
