using Dashboard.Application.Dtos;

namespace Dashboard.Application.ServiceInterfaces;

public interface IPortfolioValueService
{
    Task<List<HoldingInfo>> GetTopHoldingsByValueAsync(int count = 3);
    Task<List<HoldingInfo>> GetAllHoldingsAsync();
}
