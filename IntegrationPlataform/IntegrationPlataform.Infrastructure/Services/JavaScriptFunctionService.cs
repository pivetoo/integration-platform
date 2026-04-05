using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.JavaScriptFunctions;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class JavaScriptFunctionService : CrudService<JavaScriptFunction>, IJavaScriptFunctionService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public JavaScriptFunctionService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
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
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            JavaScriptFunction? function = await DbContext.Set<JavaScriptFunction>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (function is null)
            {
                throw new InvalidOperationException(Localizer["javaScriptFunction.notFound"]);
            }

            function.Update(request.Name, request.Code, request.Description);

            JavaScriptFunction? result = await Update(function, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        public override async Task<JavaScriptFunction?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            JavaScriptFunction? function = await DbContext.Set<JavaScriptFunction>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (function is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["javaScriptFunction.notFound"]));
                return null;
            }

            return await Delete([function], cancellationToken) ? function : null;
        }
    }
}
