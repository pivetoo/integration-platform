using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Requests.JavaScriptFunctions;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class JavaScriptFunctionService : CrudService<JavaScriptFunction>, IJavaScriptFunctionService
    {
        public JavaScriptFunctionService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<JavaScriptFunction> CreateJavaScriptFunction(CreateJavaScriptFunctionRequest request, CancellationToken cancellationToken = default)
        {
            JavaScriptFunction function = new(request.Name, request.Code, request.Description);
            bool success = await Insert(cancellationToken, function);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return function;
        }

        public async Task<JavaScriptFunction> UpdateJavaScriptFunction(long id, UpdateJavaScriptFunctionRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            JavaScriptFunction? function = await DbContext.Set<JavaScriptFunction>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (function is null)
            {
                throw new InvalidOperationException("JavaScript function not found.");
            }

            function.Update(request.Name, request.Code, request.Description);

            JavaScriptFunction? result = await Update(function, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
