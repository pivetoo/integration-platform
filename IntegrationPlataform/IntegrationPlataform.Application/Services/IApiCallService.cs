using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.ApiCalls;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IApiCallService : ICrudService<ApiCall>
    {
        Task<PagedResult<ApiCall>> GetApiCalls(PagedRequest request, CancellationToken cancellationToken = default);

        Task<ApiCall?> GetApiCallById(long id, CancellationToken cancellationToken = default);

        Task<ApiCall> CreateApiCall(CreateApiCallRequest request, CancellationToken cancellationToken = default);

        Task<ApiCall> UpdateApiCall(long id, UpdateApiCallRequest request, CancellationToken cancellationToken = default);
    }
}
