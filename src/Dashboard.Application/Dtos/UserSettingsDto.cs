namespace Dashboard.Application.Dtos;

public class UserSettingsDto
{
    public string RiskTolerance { get; set; } = "moderate";
    public string InvestmentHorizon { get; set; } = "long";
    public string CustomInstructions { get; set; } = string.Empty;
}
