using Azure;
using Kvandijk.Portfolio.Application.Dtos;
using Kvandijk.Portfolio.Domain.Entities;
using Kvandijk.Portfolio.Domain.Utils;

namespace Kvandijk.Portfolio.Application.Mappers;

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
