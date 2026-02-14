# Ticker API Reference

The Ticker API is a custom-built API that provides real-time and historical market data for stock tickers using Yahoo Finance as the data source. The Portfolio Insight Dashboard integrates with this API to fetch market information.

## Configuration

### Base Configuration

| Setting | Environment Variable | Description |
|---|---|---|
| Base URL | `TICKER_API_URL` | Base URL of the Ticker API |
| Authentication | `TICKER_API_CODE` | API authentication code/key |

**Example Configuration**:
```bash
export TICKER_API_URL="https://your-ticker-api.azurewebsites.net"
export TICKER_API_CODE="your-api-authentication-code"
```

### Caching Strategy

The dashboard implements memory caching to reduce API calls and improve performance:

- **Sliding Expiration**: 10 minutes (configurable in `StaticDetails.cs`)
- **Absolute Expiration**: 60 minutes (configurable in `StaticDetails.cs`)
- **Cache Key Format**: `history:{ticker}:{period}:{interval}`

**Configuration** (`src/Dashboard.Domain/Utils/StaticDetails.cs`):
```csharp
public static int CacheSlidingExpirationMinutes = 10;
public static int CacheAbsoluteExpirationMinutes = 60;
```

## Endpoints

### GET Market History

Fetches historical price data for a specific ticker symbol.

#### Request

**HTTP Method**: `GET`

**Endpoint**: `{TICKER_API_URL}/market-history`

**Query Parameters**:

| Parameter | Type | Required | Description | Example Values |
|---|---|---|---|---|
| `ticker` | string | ✅ Yes | Stock ticker symbol | `AAPL`, `GOOGL`, `MSFT` |
| `period` | string | ❌ No | Time period for historical data | `1mo`, `3mo`, `6mo`, `1y`, `max` |
| `interval` | string | ❌ No | Data interval (default: `1d`) | `1d`, `1wk`, `1mo` |

**Authentication**: API code passed via `code` query parameter or header (depending on API implementation).

#### Response

**Status Code**: `200 OK`

**Content-Type**: `application/json`

**Response Structure**:
```json
{
  "ticker": "AAPL",
  "currency": "USD",
  "history": [
    {
      "ticker": "AAPL",
      "date": "2024-01-15",
      "open": 185.50,
      "close": 187.25
    },
    {
      "ticker": "AAPL",
      "date": "2024-01-16",
      "open": 187.30,
      "close": 189.10
    },
    {
      "ticker": "AAPL",
      "date": "2024-01-17",
      "open": 189.00,
      "close": 190.45
    }
  ]
}
```

**Response Fields**:

| Field | Type | Description |
|---|---|---|
| `ticker` | string | Stock ticker symbol |
| `currency` | string | Currency code (e.g., USD, EUR, GBP) |
| `history` | array | Array of historical data points |
| `history[].ticker` | string | Stock ticker symbol (repeated for each data point) |
| `history[].date` | string | Date in ISO 8601 format (YYYY-MM-DD) |
| `history[].open` | number | Opening price for the day |
| `history[].close` | number | Closing price for the day |

#### Example Request

**HTTP**:
```http
GET https://your-ticker-api.azurewebsites.net/market-history?ticker=AAPL&period=1y&interval=1d
```

**cURL**:
```bash
curl -X GET "https://your-ticker-api.azurewebsites.net/market-history?ticker=AAPL&period=1y&interval=1d"
```

#### Example Response

```json
{
  "ticker": "AAPL",
  "currency": "USD",
  "history": [
    {
      "ticker": "AAPL",
      "date": "2023-01-03",
      "open": 130.28,
      "close": 125.07
    },
    {
      "ticker": "AAPL",
      "date": "2023-01-04",
      "open": 126.89,
      "close": 126.36
    },
    {
      "ticker": "AAPL",
      "date": "2023-01-05",
      "open": 127.13,
      "close": 125.02
    }
  ]
}
```

## Integration

### Service Implementation

The dashboard uses `TickerApiService` to interact with the Ticker API.

**Location**: `src/Dashboard.Infrastructure/Services/TickerApiService.cs`

**Interface**: `ITickerApiService` (defined in `src/Dashboard.Application/Interfaces/`)

### Usage in Controllers

**Example** (`src/Dashboard._Web/Controllers/DashboardController.cs`):

```csharp
public class DashboardController : Controller
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public DashboardController(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    private async Task<MarketHistoryResponseDto?> GetMarketHistoryForTickerAsync(
        string ticker, 
        string period)
    {
        using var scope = _scopeFactory.CreateScope();
        var tickerApiService = scope.ServiceProvider.GetRequiredService<ITickerApiService>();
        
        return await tickerApiService.GetMarketHistoryResponseAsync(
            ticker: ticker,
            period: period,
            interval: "1d"
        );
    }
    
    public async Task<IActionResult> DashboardContent()
    {
        var tickers = new[] { "AAPL", "GOOGL", "MSFT" };
        
        // Fetch market history for multiple tickers in parallel
        var historyTasks = tickers.Select(t => 
            GetMarketHistoryForTickerAsync(t, "1y"));
        var histories = await Task.WhenAll(historyTasks);
        
        // Process results
        foreach (var history in histories.Where(h => h != null))
        {
            Console.WriteLine($"{history.Ticker}: {history.History.Count} data points");
        }
        
        return View();
    }
}
```

### Direct Service Usage

```csharp
// Inject ITickerApiService
private readonly ITickerApiService _tickerApiService;

public MyController(ITickerApiService tickerApiService)
{
    _tickerApiService = tickerApiService;
}

// Fetch market history
public async Task<IActionResult> GetData(string ticker)
{
    var response = await _tickerApiService.GetMarketHistoryResponseAsync(
        ticker: ticker,
        period: "1mo",
        interval: "1d"
    );
    
    if (response == null || response.History == null)
    {
        return NotFound("Market data not available");
    }
    
    return Json(response);
}
```

## Period Mapping

The dashboard uses `PeriodHelper` to convert UI time ranges to API period parameters.

**Mapping Table**:

| UI Selection | API Period | Description |
|---|---|---|
| `1M` | `1mo` | 1 month of data |
| `6M` | `6mo` | 6 months of data |
| `1Y` | `1y` | 1 year of data |
| `ALL` | `max` | All available data |

**Helper Method** (`src/Dashboard.Application/Helpers/PeriodHelper.cs`):
```csharp
public static string MapPeriodToApiPeriod(string period)
{
    return period switch
    {
        "1M" => "1mo",
        "6M" => "6mo",
        "1Y" => "1y",
        "ALL" => "max",
        _ => "1y" // Default
    };
}
```

## Error Handling

### API Failures

The service returns `null` when API calls fail. Controllers should handle this gracefully:

```csharp
var response = await _tickerApiService.GetMarketHistoryResponseAsync(ticker, period, interval);

if (response == null)
{
    _logger.LogWarning("Failed to fetch market history for ticker {Ticker}", ticker);
    return PartialView("_ErrorMessage", "Unable to load market data");
}
```

### Common Error Scenarios

| Scenario | Behavior | Mitigation |
|---|---|---|
| Invalid ticker | Returns `null` | Validate ticker before calling API |
| API timeout | Returns `null` | Caching reduces timeout impact |
| Rate limiting | Returns `null` | Caching prevents excessive calls |
| Network error | Returns `null` | Retry logic (not currently implemented) |

### Logging

Failed API requests are logged via Serilog:

```csharp
_logger.LogError("Error fetching market history for {Ticker}: {Error}", ticker, ex.Message);
```

## Performance Features

### Concurrent API Calls

Multiple ticker symbols are fetched in parallel using `Task.WhenAll`:

```csharp
var historyTasks = tickers.Select(t => GetMarketHistoryForTickerAsync(t, period));
var histories = await Task.WhenAll(historyTasks);
```

**Benefits**:
- Reduced total request time
- Better user experience
- Efficient use of network resources

### Memory Caching

Cache implementation reduces redundant API calls:

**Cache Hit**:
```
Request → Check Cache → Return Cached Data (instant)
```

**Cache Miss**:
```
Request → Check Cache → API Call → Cache Result → Return Data
```

**Cache Invalidation**:
- Automatic: Sliding expiration (10 min), absolute expiration (60 min)
- Manual: Not implemented (data is read-only from API perspective)

### Request Timing

The service logs request duration for performance monitoring:

```csharp
var stopwatch = Stopwatch.StartNew();
var response = await httpClient.GetAsync(requestUri);
stopwatch.Stop();

_logger.LogInformation(
    "Ticker API request for {Ticker} completed in {Elapsed}ms",
    ticker,
    stopwatch.ElapsedMilliseconds
);
```

## Buffer Days

When fetching historical data based on transaction dates, the API adds a buffer to ensure sufficient context:

**Configuration** (`src/Dashboard.Domain/Utils/StaticDetails.cs`):
```csharp
public const int BufferDays = 7;
```

**Usage**:
```csharp
var startDate = earliestTransaction.Date.AddDays(-StaticDetails.BufferDays);
```

This ensures charts display data before the first transaction for context.

## Rate Limiting

### Current Implementation

No explicit rate limiting is implemented in the dashboard. Caching effectively reduces API call frequency.

### Recommendations for Production

1. **Implement retry logic with exponential backoff**
2. **Add circuit breaker pattern** for API resilience
3. **Monitor API usage** via Application Insights
4. **Implement request throttling** if API has strict limits
5. **Consider batch requests** if API supports it

## Testing

### Mocking the Service

For unit tests, mock `ITickerApiService`:

```csharp
var mockService = new Mock<ITickerApiService>();
mockService
    .Setup(s => s.GetMarketHistoryResponseAsync("AAPL", "1y", "1d"))
    .ReturnsAsync(new MarketHistoryResponseDto
    {
        Ticker = "AAPL",
        Currency = "USD",
        History = new List<MarketHistoryDto>
        {
            new() { Ticker = "AAPL", Date = "2024-01-01", Open = 180, Close = 185 }
        }
    });

var controller = new DashboardController(mockService.Object);
```

### Integration Testing

Test against a real or mock API endpoint:

```csharp
[Fact]
public async Task GetMarketHistory_ValidTicker_ReturnsData()
{
    // Arrange
    var service = new TickerApiService(httpClientFactory, cache, logger);
    
    // Act
    var result = await service.GetMarketHistoryResponseAsync("AAPL", "1mo", "1d");
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("AAPL", result.Ticker);
    Assert.NotEmpty(result.History);
}
```

## Troubleshooting

### No Data Returned

**Symptoms**: API returns `null` or empty history

**Solutions**:
1. Verify ticker symbol is valid
2. Check API authentication code
3. Verify API URL is correct
4. Check API logs for errors
5. Test API endpoint directly with cURL

### Performance Issues

**Symptoms**: Slow page loads, timeouts

**Solutions**:
1. Check cache hit rate in logs
2. Verify cache durations are appropriate
3. Consider increasing cache expiration
4. Verify parallel fetching is working
5. Check network latency to API

### Cache Issues

**Symptoms**: Stale data, incorrect values

**Solutions**:
1. Verify cache key generation
2. Check cache expiration settings
3. Clear cache by restarting application
4. Review cache invalidation logic

---

[← Back to Documentation Index](./README.md)
