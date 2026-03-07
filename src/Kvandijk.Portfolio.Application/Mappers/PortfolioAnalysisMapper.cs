using Azure;
using Kvandijk.Portfolio.Application.Dtos;
using Kvandijk.Portfolio.Application.Helpers;
using Kvandijk.Portfolio.Domain.Entities;
using Kvandijk.Portfolio.Domain.Utils;

namespace Kvandijk.Portfolio.Application.Mappers;

public static class PortfolioAnalysisMapper
{
    public static PortfolioAnalysisEntity ToEntity(this PortfolioAnalysisDto dto, string portfolioSnapshot = "")
    {
        return new PortfolioAnalysisEntity
        {
            PartitionKey = StaticDetails.AiAnalysesPartitionKey,
            RowKey = string.IsNullOrWhiteSpace(dto.RowKey) ? Guid.NewGuid().ToString("N") : dto.RowKey,
            AnalysisDate = FormattingHelper.FormatDate(dto.AnalysisDate),
            AnalysisType = dto.AnalysisType,
            WeekNumber = dto.WeekNumber.ToString(),
            Content = dto.Content,
            PortfolioSnapshot = portfolioSnapshot,
            ETag = ETag.All
        };
    }

    public static PortfolioAnalysisDto ToDto(this PortfolioAnalysisEntity entity)
    {
        return new PortfolioAnalysisDto
        {
            RowKey = entity.RowKey,
            AnalysisDate = FormattingHelper.ParseDateOnly(entity.AnalysisDate),
            AnalysisType = entity.AnalysisType,
            WeekNumber = int.TryParse(entity.WeekNumber, out var week) ? week : 0,
            Content = entity.Content
        };
    }
}
