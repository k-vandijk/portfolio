using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Dashboard.Application.Dtos;
using Dashboard.Application.Mappers;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Application.ServiceInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using System.Text.Json;

namespace Dashboard.Infrastructure.Services;

#pragma warning disable OPENAI001
#pragma warning disable CA2252

public class PortfolioAnalysisService : IPortfolioAnalysisService
{
    private readonly IPortfolioAnalysesRepository _analysesRepository;
    private readonly IPortfolioValueService _portfolioValueService;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly ITransactionsRepository _transactionsRepository;
    private readonly IConfiguration _config;
    private readonly ILogger<PortfolioAnalysisService> _logger;

    public PortfolioAnalysisService(
        IPortfolioAnalysesRepository analysesRepository,
        IPortfolioValueService portfolioValueService,
        IUserSettingsRepository userSettingsRepository,
        ITransactionsRepository transactionsRepository,
        IConfiguration config,
        ILogger<PortfolioAnalysisService> logger)
    {
        _analysesRepository = analysesRepository;
        _portfolioValueService = portfolioValueService;
        _userSettingsRepository = userSettingsRepository;
        _transactionsRepository = transactionsRepository;
        _config = config;
        _logger = logger;
    }

    public async Task RunWeeklyAnalysisAsync()
    {
        _logger.LogInformation("Starting weekly portfolio analysis");

        var settingsEntities = await _userSettingsRepository.GetAllAsync();
        var settings = settingsEntities.FirstOrDefault()?.ToDto() ?? new UserSettingsDto();

        var holdings = await _portfolioValueService.GetAllHoldingsAsync();

        if (holdings.Count == 0)
        {
            _logger.LogInformation("No holdings found, skipping weekly analysis");
            return;
        }

        var transactionEntities = await _transactionsRepository.GetAllAsync();
        var transactions = transactionEntities.Select(e => e.ToModel()).ToList();

        var previousAnalyses = await GetWeeklyAnalysesForCurrentMonthAsync();
        var weekNumber = previousAnalyses.Count + 1;

        var prompt = PortfolioAnalysisPromptBuilder.BuildWeeklyPrompt(holdings, transactions, settings, previousAnalyses, weekNumber);
        var content = await InvokeAgentAsync(prompt);

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

        await _analysesRepository.AddAsync(dto.ToEntity(portfolioSnapshot));

        _logger.LogInformation("Weekly portfolio analysis (week {Week}) saved", weekNumber);
    }

    public async Task<PortfolioAnalysisDto> GenerateMonthlyReportAsync()
    {
        _logger.LogInformation("Generating monthly portfolio report");

        var settingsEntities = await _userSettingsRepository.GetAllAsync();
        var settings = settingsEntities.FirstOrDefault()?.ToDto() ?? new UserSettingsDto();

        var holdings = await _portfolioValueService.GetAllHoldingsAsync();
        var weeklyAnalyses = await GetWeeklyAnalysesForCurrentMonthAsync();

        var prompt = PortfolioAnalysisPromptBuilder.BuildMonthlyPrompt(holdings, weeklyAnalyses, settings);
        var content = await InvokeAgentAsync(prompt);

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
        await _analysesRepository.AddAsync(entity);
        dto.RowKey = entity.RowKey;

        _logger.LogInformation("Monthly portfolio report saved");
        return dto;
    }

    public async Task<string> ChatAsync(string userMessage)
    {
        var settingsEntities = await _userSettingsRepository.GetAllAsync();
        var settings = settingsEntities.FirstOrDefault()?.ToDto() ?? new UserSettingsDto();

        var holdings = await _portfolioValueService.GetAllHoldingsAsync();
        
        var recentAnalysesEntities = await _analysesRepository.GetRecentAnalysesAsync(3);
        var recentAnalyses = recentAnalysesEntities.Select(e => e.ToDto()).ToList();

        var transactionEntities = await _transactionsRepository.GetAllAsync();
        var transactions = transactionEntities.Select(e => e.ToModel()).ToList();

        var prompt = PortfolioAnalysisPromptBuilder.BuildChatPrompt(holdings, transactions, settings, recentAnalyses, userMessage);
        return await InvokeAgentAsync(prompt);
    }

    private async Task<List<PortfolioAnalysisDto>> GetWeeklyAnalysesForCurrentMonthAsync()
    {
        var entities = await _analysesRepository.GetWeeklyForCurrentMonthAsync();
        return entities.OrderBy(e => e.AnalysisDate).Select(e => e.ToDto()).ToList();
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
}
