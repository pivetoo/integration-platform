using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IServiceCallbackDispatcher
    {
        Task<ServiceCallbackResult> DispatchAsync(Execution execution, CancellationToken cancellationToken = default);
    }

    public sealed record ServiceCallbackResult(bool Attempted, bool Success, string? Detail);
}
