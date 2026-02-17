using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface IPortfolioValueService
{
    Task<List<HoldingInfo>> GetTopHoldingsByValueAsync(int count = 3);
}
