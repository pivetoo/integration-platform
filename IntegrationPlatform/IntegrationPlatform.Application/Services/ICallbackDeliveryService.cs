using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface ICallbackDeliveryService
    {
        Task<IReadOnlyCollection<CallbackDelivery>> GetPendingToDeliver(int maxItems, CancellationToken cancellationToken = default);

        Task DeliverItem(long callbackDeliveryId, CancellationToken cancellationToken = default);
    }
}
