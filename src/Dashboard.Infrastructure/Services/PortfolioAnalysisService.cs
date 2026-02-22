using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Azure.Data.Tables;
using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Dashboard.Application.Mappers;
using Dashboard.Domain.Models;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dashboard.Infrastructure.Services;

public class PortfolioAnalysisService : IPortfolioAnalysisService
{
    private readonly TableClient _table;
    private readonly ITransactionService _transactionService;
    private readonly IPortfolioValueService _portfolioValueService;
    private readonly ITickerApiService _tickerApiService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IConfiguration _config;
    private readonly ILogger<PortfolioAnalysisService> _logger;

    public PortfolioAnalysisService(
        [FromKeyedServices(StaticDetails.AiAnalysesTableName)] TableClient table,
        ITransactionService transactionService,
        IPortfolioValueService portfolioValueService,
        ITickerApiService tickerApiService,
        IUserSettingsService userSettingsService,
        IConfiguration config,
        ILogger<PortfolioAnalysisService> logger)
    {
        _table = table;
        _transactionService = transactionService;
        _portfolioValueService = portfolioValueService;
        _tickerApiService = tickerApiService;
        _userSettingsService = userSettingsService;
        _config = config;
        _logger = logger;
    }

    public async Task<List<PortfolioAnalysisDto>> GetRecentAnalysesAsync(int count = 4)
    {
        var filter = $"PartitionKey eq '{StaticDetails.AiAnalysesPartitionKey}'";
        var entities = new List<PortfolioAnalysisEntity>();

        await foreach (var entity in _table.QueryAsync<PortfolioAnalysisEntity>(filter: filter))
            entities.Add(entity);

        return entities
            .OrderByDescending(e => e.AnalysisDate)
            .Take(count)
            .Select(e => e.ToDto())
            .ToList();
    }

    public async Task RunWeeklyAnalysisAsync()
    {
        _logger.LogInformation("Starting weekly portfolio analysis");

        var settings = await _userSettingsService.GetSettingsAsync();
        var transactions = await _transactionService.GetTransactionsAsync();
        var holdings = await _portfolioValueService.GetAllHoldingsAsync();

        if (holdings.Count == 0)
        {
            _logger.LogInformation("No holdings found, skipping weekly analysis");
            return;
        }

        // Fetch 30-day market history for each held ticker
        var tickers = holdings.Select(h => h.Ticker).ToList();
        var marketHistory = await FetchMarketHistoryAsync(tickers, "1mo");

        // Load previous analyses from this month for continuity
        var previousAnalyses = await GetWeeklyAnalysesForCurrentMonthAsync();
        var weekNumber = previousAnalyses.Count + 1;

        var systemPrompt = BuildSystemPrompt(settings, weekNumber, previousAnalyses.Count > 0);
        var userPrompt = BuildWeeklyUserPrompt(holdings, transactions, marketHistory, previousAnalyses, weekNumber);

        var content = await CallAiFoundryAsync(systemPrompt, userPrompt);

        var portfolioSnapshot = JsonSerializer.Serialize(holdings.Select(h => new
        {
            h.Ticker,
            h.Quantity,
            h.CurrentPrice,
            h.TotalValue
        }));

        var dto = new PortfolioAnalysisDto
        {
            AnalysisDate = DateOnly.FromDateTime(DateTime.Today),
            AnalysisType = "weekly",
            WeekNumber = weekNumber,
            Content = content
        };

        var entity = dto.ToEntity(portfolioSnapshot);
        await _table.AddEntityAsync(entity);

        _logger.LogInformation("Weekly portfolio analysis (week {Week}) saved", weekNumber);
    }

    public async Task<PortfolioAnalysisDto> GenerateMonthlyReportAsync()
    {
        _logger.LogInformation("Generating monthly portfolio report");

        var settings = await _userSettingsService.GetSettingsAsync();
        var holdings = await _portfolioValueService.GetAllHoldingsAsync();
        var weeklyAnalyses = await GetWeeklyAnalysesForCurrentMonthAsync();

        var systemPrompt = BuildMonthlySystemPrompt(settings);
        var userPrompt = BuildMonthlyUserPrompt(holdings, weeklyAnalyses);

        var content = await CallAiFoundryAsync(systemPrompt, userPrompt);

        var portfolioSnapshot = JsonSerializer.Serialize(holdings.Select(h => new
        {
            h.Ticker,
            h.Quantity,
            h.CurrentPrice,
            h.TotalValue
        }));

        var dto = new PortfolioAnalysisDto
        {
            AnalysisDate = DateOnly.FromDateTime(DateTime.Today),
            AnalysisType = "monthly",
            WeekNumber = 0,
            Content = content
        };

        var entity = dto.ToEntity(portfolioSnapshot);
        await _table.AddEntityAsync(entity);
        dto.RowKey = entity.RowKey;

        _logger.LogInformation("Monthly portfolio report saved");
        return dto;
    }

    private async Task<List<PortfolioAnalysisDto>> GetWeeklyAnalysesForCurrentMonthAsync()
    {
        var startOfMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
        var filter = $"PartitionKey eq '{StaticDetails.AiAnalysesPartitionKey}' and AnalysisDate ge '{startOfMonth}' and AnalysisType eq 'weekly'";

        var entities = new List<PortfolioAnalysisEntity>();
        await foreach (var entity in _table.QueryAsync<PortfolioAnalysisEntity>(filter: filter))
            entities.Add(entity);

        return entities
            .OrderBy(e => e.AnalysisDate)
            .Select(e => e.ToDto())
            .ToList();
    }

    private async Task<Dictionary<string, List<(DateOnly Date, decimal Open, decimal Close)>>> FetchMarketHistoryAsync(
        List<string> tickers, string period)
    {
        var result = new Dictionary<string, List<(DateOnly, decimal, decimal)>>();

        foreach (var ticker in tickers)
        {
            try
            {
                var data = await _tickerApiService.GetMarketHistoryResponseAsync(ticker, period);
                if (data?.History is { Count: > 0 })
                {
                    result[ticker] = data.History
                        .OrderBy(h => h.Date)
                        .Select(h => (h.Date, h.Open, h.Close))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch market history for {Ticker}", ticker);
            }
        }

        return result;
    }

    private async Task<string> CallAiFoundryAsync(string systemPrompt, string userPrompt)
    {
        var endpoint = _config["azure-foundry-endpoint"] ?? throw new InvalidOperationException("azure-foundry-endpoint is not configured");
        var key = _config["azure-foundry-key"] ?? throw new InvalidOperationException("azure-foundry-key is not configured");
        var deployment = _config["azure-foundry-deployment"] ?? throw new InvalidOperationException("azure-foundry-deployment is not configured");

        var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
        var chatClient = client.GetChatClient(deployment);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        var response = await chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }

    private static string BuildSystemPrompt(UserSettingsDto settings, int weekNumber, bool hasPreviousAnalyses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an experienced, objective portfolio advisor. Your role is to analyse investment data and provide clear, actionable narrative insights.");
        sb.AppendLine();
        sb.AppendLine("## User Investment Profile");
        sb.AppendLine($"- Risk tolerance: {settings.RiskTolerance}");
        sb.AppendLine($"- Investment horizon: {settings.InvestmentHorizon}");

        if (!string.IsNullOrWhiteSpace(settings.CustomInstructions))
        {
            sb.AppendLine($"- Additional preferences: {settings.CustomInstructions}");
        }

        sb.AppendLine();
        sb.AppendLine($"This is week {weekNumber} of 4 for the current month.");

        if (hasPreviousAnalyses)
            sb.AppendLine("Previous weekly analyses are included below for continuity — reference them when assessing trends or changes.");

        sb.AppendLine();
        sb.AppendLine("Write in clear, plain English. Avoid jargon. Be direct but considerate of the user's risk profile.");
        sb.AppendLine("Do not make specific buy/sell recommendations with exact amounts. Focus on observations, patterns, and whether rebalancing signals are forming.");

        return sb.ToString();
    }

    private static string BuildWeeklyUserPrompt(
        List<HoldingInfo> holdings,
        List<TransactionDto> transactions,
        Dictionary<string, List<(DateOnly Date, decimal Open, decimal Close)>> marketHistory,
        List<PortfolioAnalysisDto> previousAnalyses,
        int weekNumber)
    {
        var sb = new StringBuilder();
        var totalValue = holdings.Sum(h => h.TotalValue);

        sb.AppendLine("## Current Portfolio Holdings");
        sb.AppendLine($"Total portfolio value: {totalValue:C2}");
        sb.AppendLine();
        sb.AppendLine("| Ticker | Quantity | Current Price | Total Value | Portfolio % |");
        sb.AppendLine("|--------|----------|---------------|-------------|-------------|");

        foreach (var h in holdings)
        {
            var pct = totalValue > 0 ? h.TotalValue / totalValue * 100m : 0m;
            sb.AppendLine($"| {h.Ticker} | {h.Quantity:F4} | {h.CurrentPrice:C2} | {h.TotalValue:C2} | {pct:F1}% |");
        }

        sb.AppendLine();
        sb.AppendLine("## All-Time Transactions");
        sb.AppendLine("| Date | Ticker | Quantity | Purchase Price | Total Cost |");
        sb.AppendLine("|------|--------|----------|----------------|------------|");

        foreach (var t in transactions.OrderBy(t => t.Date))
            sb.AppendLine($"| {t.Date:yyyy-MM-dd} | {t.Ticker} | {t.Amount:F4} | {t.PurchasePrice:C2} | {t.TotalCosts:C2} |");

        sb.AppendLine();
        sb.AppendLine("## Market History (Last 30 Days)");

        foreach (var (ticker, history) in marketHistory)
        {
            sb.AppendLine($"### {ticker}");
            sb.AppendLine("| Date | Open | Close |");
            sb.AppendLine("|------|------|-------|");
            foreach (var (date, open, close) in history)
                sb.AppendLine($"| {date:yyyy-MM-dd} | {open:C2} | {close:C2} |");
            sb.AppendLine();
        }

        if (previousAnalyses.Count > 0)
        {
            sb.AppendLine("## Previous Weekly Analyses (This Month)");
            foreach (var prev in previousAnalyses)
            {
                sb.AppendLine($"### Week {prev.WeekNumber} — {prev.AnalysisDate:MMMM d, yyyy}");
                sb.AppendLine(prev.Content);
                sb.AppendLine();
            }
        }

        sb.AppendLine($"## Your Task");
        sb.AppendLine($"Provide a narrative portfolio analysis for week {weekNumber} of this month, covering:");
        sb.AppendLine("1. Portfolio health and concentration risk");
        sb.AppendLine("2. How holdings have performed since last week (or since inception for week 1)");
        sb.AppendLine("3. Notable market conditions for each holding based on the price history");
        sb.AppendLine("4. Whether any rebalancing signals are beginning to emerge");
        sb.AppendLine("5. Key things to watch next week");

        return sb.ToString();
    }

    private static string BuildMonthlySystemPrompt(UserSettingsDto settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an experienced, objective portfolio advisor. Your role is to synthesise a month of weekly analyses into a clear, actionable monthly rebalancing assessment.");
        sb.AppendLine();
        sb.AppendLine("## User Investment Profile");
        sb.AppendLine($"- Risk tolerance: {settings.RiskTolerance}");
        sb.AppendLine($"- Investment horizon: {settings.InvestmentHorizon}");

        if (!string.IsNullOrWhiteSpace(settings.CustomInstructions))
            sb.AppendLine($"- Additional preferences: {settings.CustomInstructions}");

        sb.AppendLine();
        sb.AppendLine("Write in clear, plain English. Be specific in your reasoning but do not prescribe exact quantities to trade.");
        sb.AppendLine("The user will read this report right before their monthly investment and rebalancing session.");

        return sb.ToString();
    }

    private static string BuildMonthlyUserPrompt(
        List<HoldingInfo> holdings,
        List<PortfolioAnalysisDto> weeklyAnalyses)
    {
        var sb = new StringBuilder();
        var totalValue = holdings.Sum(h => h.TotalValue);

        sb.AppendLine("## Current Portfolio State");
        sb.AppendLine($"Total portfolio value: {totalValue:C2}");
        sb.AppendLine();
        sb.AppendLine("| Ticker | Quantity | Current Price | Total Value | Portfolio % |");
        sb.AppendLine("|--------|----------|---------------|-------------|-------------|");

        foreach (var h in holdings)
        {
            var pct = totalValue > 0 ? h.TotalValue / totalValue * 100m : 0m;
            sb.AppendLine($"| {h.Ticker} | {h.Quantity:F4} | {h.CurrentPrice:C2} | {h.TotalValue:C2} | {pct:F1}% |");
        }

        sb.AppendLine();
        sb.AppendLine("## Weekly Analyses From This Month");

        foreach (var analysis in weeklyAnalyses.OrderBy(a => a.WeekNumber))
        {
            sb.AppendLine($"### Week {analysis.WeekNumber} — {analysis.AnalysisDate:MMMM d, yyyy}");
            sb.AppendLine(analysis.Content);
            sb.AppendLine();
        }

        sb.AppendLine("## Your Task");
        sb.AppendLine("Synthesise the four weekly analyses into a final monthly rebalancing assessment covering:");
        sb.AppendLine("1. A summary of the month's key portfolio developments");
        sb.AppendLine("2. Which holdings are over- or under-represented relative to the user's risk profile");
        sb.AppendLine("3. Recurring concerns or themes from the weekly analyses");
        sb.AppendLine("4. Clear reasoning for whether rebalancing is warranted this month");
        sb.AppendLine("5. Suggested focus areas for new investment capital, if any");

        return sb.ToString();
    }
}
