using System.Text;
using Dashboard.Application.Dtos;

namespace Dashboard.Infrastructure.Services;

public static class PortfolioAnalysisPromptBuilder
{
    public static string BuildWeeklyPrompt(
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

    public static string BuildMonthlyPrompt(
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

    public static string BuildChatPrompt(
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
}
