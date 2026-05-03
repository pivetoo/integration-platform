using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Application.Requests.JavaScriptFunctions;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IJavaScriptFunctionService : ICrudService<JavaScriptFunction>
    {
        Task<PagedResult<JavaScriptFunction>> GetJavaScriptFunctions(PagedRequest request, CancellationToken cancellationToken = default);

        Task<JavaScriptFunction?> GetJavaScriptFunctionById(long id, CancellationToken cancellationToken = default);

        Task<JavaScriptFunction> CreateJavaScriptFunction(CreateJavaScriptFunctionRequest request, CancellationToken cancellationToken = default);

        Task<JavaScriptFunction> UpdateJavaScriptFunction(long id, UpdateJavaScriptFunctionRequest request, CancellationToken cancellationToken = default);
    }
}
