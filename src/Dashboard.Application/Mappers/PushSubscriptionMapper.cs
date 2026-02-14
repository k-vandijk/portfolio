using Azure;
using Dashboard.Application.Dtos;
using Dashboard.Domain.Models;
using Dashboard.Domain.Utils;

namespace Dashboard.Application.Mappers;

public static class PushSubscriptionMapper
{
    public static PushSubscriptionEntity ToEntity(this PushSubscriptionDto dto)
    {
        return new PushSubscriptionEntity
        {
            PartitionKey = StaticDetails.PushSubscriptionsPartitionKey,
            RowKey = Guid.NewGuid().ToString("N"),
            Endpoint = dto.Endpoint,
            P256dh = dto.P256dh,
            Auth = dto.Auth,
            ETag = ETag.All
        };
    }

    public static PushSubscriptionDto ToDto(this PushSubscriptionEntity entity)
    {
        return new PushSubscriptionDto
        {
            Endpoint = entity.Endpoint,
            P256dh = entity.P256dh,
            Auth = entity.Auth
        };
    }
}
