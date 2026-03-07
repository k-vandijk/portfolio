using Azure;
using Dashboard.Application.Dtos;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;

namespace Dashboard.Application.Mappers;

public static class UserSettingsMapper
{
    public static UserSettingsEntity ToEntity(this UserSettingsDto dto)
    {
        return new UserSettingsEntity
        {
            PartitionKey = StaticDetails.UserSettingsPartitionKey,
            RowKey = StaticDetails.UserSettingsRowKey,
            RiskTolerance = dto.RiskTolerance,
            InvestmentHorizon = dto.InvestmentHorizon,
            CustomInstructions = dto.CustomInstructions,
            ETag = ETag.All
        };
    }

    public static UserSettingsDto ToDto(this UserSettingsEntity entity)
    {
        return new UserSettingsDto
        {
            RiskTolerance = entity.RiskTolerance,
            InvestmentHorizon = entity.InvestmentHorizon,
            CustomInstructions = entity.CustomInstructions
        };
    }
}
