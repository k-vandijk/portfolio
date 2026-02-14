namespace Dashboard.Application.Interfaces;

public interface IPortfolioValueService
{
    Task<decimal> GetCurrentPortfolioWorthAsync();
}
