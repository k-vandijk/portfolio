using Kvandijk.Portfolio.Application.Dtos;

namespace Kvandijk.Portfolio.Application.ServiceInterfaces;

public interface IPortfolioValueService
{
    Task<List<HoldingInfo>> GetTopHoldingsByValueAsync(int count = 3);
    Task<List<HoldingInfo>> GetAllHoldingsAsync();
}
