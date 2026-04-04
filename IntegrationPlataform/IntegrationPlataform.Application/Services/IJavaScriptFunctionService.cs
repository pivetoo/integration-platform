using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.JavaScriptFunctions;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IJavaScriptFunctionService : ICrudService<JavaScriptFunction>
    {
        Task<JavaScriptFunction> CreateJavaScriptFunction(CreateJavaScriptFunctionRequest request, CancellationToken cancellationToken = default);

        Task<JavaScriptFunction> UpdateJavaScriptFunction(long id, UpdateJavaScriptFunctionRequest request, CancellationToken cancellationToken = default);
    }
}
