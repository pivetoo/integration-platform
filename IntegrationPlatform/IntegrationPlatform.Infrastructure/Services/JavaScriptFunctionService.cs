using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.JavaScriptFunctions;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class JavaScriptFunctionService : CrudService<JavaScriptFunction>, IJavaScriptFunctionService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public JavaScriptFunctionService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<JavaScriptFunction>> GetJavaScriptFunctions(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = DbContext.Set<JavaScriptFunction>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower) || (item.Description != null && item.Description.ToLower().Contains(lower)));
            }
            return await query
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<JavaScriptFunction?> GetJavaScriptFunctionById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<JavaScriptFunction>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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
                throw new InvalidOperationException("request.route.idMismatch");
            }

            JavaScriptFunction? function = await DbContext.Set<JavaScriptFunction>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (function is null)
            {
                throw new InvalidOperationException("javaScriptFunction.notFound");
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
