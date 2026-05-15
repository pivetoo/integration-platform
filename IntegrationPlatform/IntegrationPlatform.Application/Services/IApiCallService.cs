using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Application.Requests.ApiCalls;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IApiCallService : ICrudService<ApiCall>
    {
        Task<PagedResult<ApiCall>> GetApiCalls(PagedRequest request, string? search, CancellationToken cancellationToken = default);

        Task<ApiCall?> GetApiCallById(long id, CancellationToken cancellationToken = default);

        Task<ApiCall> CreateApiCall(CreateApiCallRequest request, CancellationToken cancellationToken = default);

        Task<ApiCall> UpdateApiCall(long id, UpdateApiCallRequest request, CancellationToken cancellationToken = default);
    }
}
