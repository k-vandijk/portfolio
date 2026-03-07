using Azure;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Data.Tables;
using Azure.Identity;
using Dashboard.Application.Dtos;
using Dashboard.Application.Mappers;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using System.Text;
using System.Text.Json;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Application.ServiceInterfaces;
using Dashboard.Domain.Entities;

namespace Dashboard.Infrastructure.Services;

#pragma warning disable OPENAI001
#pragma warning disable CA2252

public class PortfolioAnalysisService : IPortfolioAnalysisService
{
    private readonly TableClient _table;
    private readonly IPortfolioValueService _portfolioValueService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IConfiguration _config;
    private readonly ILogger<PortfolioAnalysisService> _logger;
    private readonly ITransactionsRepository _transactionsRepository;

    public PortfolioAnalysisService(
        [FromKeyedServices(StaticDetails.AiAnalysesTableName)] TableClient table,
        IPortfolioValueService portfolioValueService,
        IUserSettingsService userSettingsService,
        IConfiguration config,
        ILogger<PortfolioAnalysisService> logger, ITransactionsRepository transactionsRepository)
    {
        _table = table;
        _portfolioValueService = portfolioValueService;
        _userSettingsService = userSettingsService;
        _config = config;
        _logger = logger;
        _transactionsRepository = transactionsRepository;
    }

    public async Task<List<PortfolioAnalysisDto>> GetAllAnalysesAsync()
    {
        var filter = $"PartitionKey eq '{StaticDetails.AiAnalysesPartitionKey}'";
        var entities = new List<PortfolioAnalysisEntity>();

        await foreach (var entity in _table.QueryAsync<PortfolioAnalysisEntity>(filter: filter))
            entities.Add(entity);

        return entities
            .OrderByDescending(e => e.AnalysisDate)
            .Select(e => e.ToDto())
            .ToList();
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
        var holdings = await _portfolioValueService.GetAllHoldingsAsync();

        var transactionEntities = await _transactionsRepository.GetAllAsync();
        var transactionDtos = transactionEntities.Select(e => e.ToModel()).ToList();

        if (holdings.Count == 0)
        {
            _logger.LogInformation("No holdings found, skipping weekly analysis");
            return;
        }

        var previousAnalyses = await GetWeeklyAnalysesForCurrentMonthAsync();
        var weekNumber = previousAnalyses.Count + 1;

        var userPrompt = BuildWeeklyUserPrompt(holdings, transactionDtos, settings, previousAnalyses, weekNumber);
        var content = await InvokeAgentAsync(userPrompt);

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

        var userPrompt = BuildMonthlyUserPrompt(holdings, weeklyAnalyses, settings);
        var content = await InvokeAgentAsync(userPrompt);

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

    public async Task DeleteAnalysisAsync(string rowKey)
    {
        await _table.DeleteEntityAsync(StaticDetails.AiAnalysesPartitionKey, rowKey, ETag.All);
        _logger.LogInformation("Deleted analysis with RowKey {RowKey}", rowKey);
    }

    public async Task<string> ChatAsync(string userMessage)
    {
        var settings = await _userSettingsService.GetSettingsAsync();
        var holdings = await _portfolioValueService.GetAllHoldingsAsync();
        var recentAnalyses = await GetRecentAnalysesAsync(3);

        var transactionEntities = await _transactionsRepository.GetAllAsync();
        var transactionDtos = transactionEntities.Select(e => e.ToModel()).ToList();

        var prompt = BuildChatPrompt(holdings, transactionDtos, settings, recentAnalyses, userMessage);
        return await InvokeAgentAsync(prompt);
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

    private async Task<string> InvokeAgentAsync(string userMessage)
    {
        var foundryEndpoint = _config["MicrosoftFoundry:Endpoint"] ?? throw new InvalidOperationException("MicrosoftFoundry:Endpoint is not configured");
        var foundryAgentName = _config["MicrosoftFoundry:AgentName"] ?? throw new InvalidOperationException("MicrosoftFoundry:AgentName is not configured");

        AIProjectClient projectClient = new(endpoint: new Uri(foundryEndpoint), tokenProvider: new DefaultAzureCredential());

        AgentRecord agentRecord = projectClient.Agents.GetAgent(foundryAgentName);
        _logger.LogInformation("Agent retrieved (name: {AgentRecordName}, id: {AgentRecordId})", agentRecord.Name, agentRecord.Id);

        ProjectResponsesClient responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentRecord);
        ResponseResult response = await responseClient.CreateResponseAsync(userMessage);

        return response.GetOutputText();
    }

    private static string BuildWeeklyUserPrompt(
        List<HoldingInfo> holdings,
        List<TransactionDto> transactions,
        UserSettingsDto settings,
        List<PortfolioAnalysisDto> previousAnalyses,
        int weekNumber)
    {
        var sb = new StringBuilder();
        var totalValue = holdings.Sum(h => h.TotalValue);
        var today = DateTime.Today;

        sb.AppendLine("## Investment Profile");
        sb.AppendLine($"- Risk tolerance: {settings.RiskTolerance}");
        sb.AppendLine($"- Investment horizon: {settings.InvestmentHorizon}");

        if (!string.IsNullOrWhiteSpace(settings.CustomInstructions))
            sb.AppendLine($"- Custom preferences: {settings.CustomInstructions}");

        sb.AppendLine();
        sb.AppendLine($"## Current Portfolio — {today:MMMM d, yyyy}");
        sb.AppendLine($"Total value: {totalValue:C2}");
        sb.AppendLine();
        sb.AppendLine("| Ticker | Quantity | Current Price | Total Value | Portfolio % |");
        sb.AppendLine("|--------|----------|---------------|-------------|-------------|");

        foreach (var h in holdings)
        {
            var pct = totalValue > 0 ? h.TotalValue / totalValue * 100m : 0m;
            sb.AppendLine($"| {h.Ticker} | {h.Quantity:F4} | {h.CurrentPrice:C2} | {h.TotalValue:C2} | {pct:F1}% |");
        }

        sb.AppendLine();
        sb.AppendLine("## Transaction History (All Time)");
        sb.AppendLine("| Date | Ticker | Quantity | Purchase Price | Total Cost |");
        sb.AppendLine("|------|--------|----------|----------------|------------|");

        foreach (var t in transactions.OrderBy(t => t.Date))
            sb.AppendLine($"| {t.Date:yyyy-MM-dd} | {t.Ticker} | {t.Amount:F4} | {t.PurchasePrice:C2} | {t.TotalCosts:C2} |");

        if (previousAnalyses.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Previous Weekly Analyses This Month");
            foreach (var prev in previousAnalyses)
            {
                sb.AppendLine($"### Week {prev.WeekNumber} — {prev.AnalysisDate:MMMM d, yyyy}");
                sb.AppendLine(prev.Content);
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Your Task");
        sb.AppendLine($"This is week {weekNumber} of 4 for {today:MMMM yyyy}. Please use web search to research");
        sb.AppendLine("current news and market conditions for each of my holdings, then provide a");
        sb.AppendLine("weekly portfolio assessment covering:");
        sb.AppendLine("1. Portfolio health and concentration risk");
        sb.AppendLine("2. Performance and notable developments for each holding");
        sb.AppendLine("3. Current market conditions and relevant news for each holding");
        sb.AppendLine("4. Whether rebalancing signals are forming");
        sb.AppendLine("5. Key things to watch next week");

        return sb.ToString();
    }

    private static string BuildChatPrompt(
        List<HoldingInfo> holdings,
        List<TransactionDto> transactions,
        UserSettingsDto settings,
        List<PortfolioAnalysisDto> recentAnalyses,
        string userMessage)
    {
        var sb = new StringBuilder();
        var totalValue = holdings.Sum(h => h.TotalValue);
        var today = DateTime.Today;

        sb.AppendLine("## Investment Profile");
        sb.AppendLine($"- Risk tolerance: {settings.RiskTolerance}");
        sb.AppendLine($"- Investment horizon: {settings.InvestmentHorizon}");

        if (!string.IsNullOrWhiteSpace(settings.CustomInstructions))
            sb.AppendLine($"- Custom preferences: {settings.CustomInstructions}");

        sb.AppendLine();
        sb.AppendLine($"## Current Portfolio — {today:MMMM d, yyyy}");
        sb.AppendLine($"Total value: {totalValue:C2}");
        sb.AppendLine();
        sb.AppendLine("| Ticker | Quantity | Current Price | Total Value | Portfolio % |");
        sb.AppendLine("|--------|----------|---------------|-------------|-------------|");

        foreach (var h in holdings)
        {
            var pct = totalValue > 0 ? h.TotalValue / totalValue * 100m : 0m;
            sb.AppendLine($"| {h.Ticker} | {h.Quantity:F4} | {h.CurrentPrice:C2} | {h.TotalValue:C2} | {pct:F1}% |");
        }

        sb.AppendLine();
        sb.AppendLine("## Transaction History (All Time)");
        sb.AppendLine("| Date | Ticker | Quantity | Purchase Price | Total Cost |");
        sb.AppendLine("|------|--------|----------|----------------|------------|");

        foreach (var t in transactions.OrderBy(t => t.Date))
            sb.AppendLine($"| {t.Date:yyyy-MM-dd} | {t.Ticker} | {t.Amount:F4} | {t.PurchasePrice:C2} | {t.TotalCosts:C2} |");

        if (recentAnalyses.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Recent Analyses (for context)");
            foreach (var analysis in recentAnalyses)
            {
                sb.AppendLine($"### {analysis.AnalysisType} — {analysis.AnalysisDate:MMMM d, yyyy}");
                sb.AppendLine(analysis.Content);
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("## User Question");
        sb.AppendLine(userMessage);

        return sb.ToString();
    }

    private static string BuildMonthlyUserPrompt(
        List<HoldingInfo> holdings,
        List<PortfolioAnalysisDto> weeklyAnalyses,
        UserSettingsDto settings)
    {
        var sb = new StringBuilder();
        var totalValue = holdings.Sum(h => h.TotalValue);
        var today = DateTime.Today;

        sb.AppendLine("## Investment Profile");
        sb.AppendLine($"- Risk tolerance: {settings.RiskTolerance}");
        sb.AppendLine($"- Investment horizon: {settings.InvestmentHorizon}");

        if (!string.IsNullOrWhiteSpace(settings.CustomInstructions))
            sb.AppendLine($"- Custom preferences: {settings.CustomInstructions}");

        sb.AppendLine();
        sb.AppendLine($"## Current Portfolio State — {today:MMMM d, yyyy}");
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
        sb.AppendLine("Use web search to check for any final breaking news, then synthesise the four weekly");
        sb.AppendLine("analyses into a final monthly rebalancing assessment covering:");
        sb.AppendLine("1. A summary of the month's key portfolio developments");
        sb.AppendLine("2. Which holdings are over- or under-represented relative to my risk profile");
        sb.AppendLine("3. Recurring concerns or themes from the weekly analyses");
        sb.AppendLine("4. Clear reasoning for whether rebalancing is warranted this month");
        sb.AppendLine("5. Suggested focus areas for new investment capital, if any");

        return sb.ToString();
    }
}
