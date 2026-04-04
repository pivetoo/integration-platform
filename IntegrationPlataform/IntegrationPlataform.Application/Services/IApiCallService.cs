using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.ApiCalls;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IApiCallService : ICrudService<ApiCall>
    {
        Task<ApiCall> CreateApiCall(CreateApiCallRequest request, CancellationToken cancellationToken = default);

        Task<ApiCall> UpdateApiCall(long id, UpdateApiCallRequest request, CancellationToken cancellationToken = default);
    }
}
