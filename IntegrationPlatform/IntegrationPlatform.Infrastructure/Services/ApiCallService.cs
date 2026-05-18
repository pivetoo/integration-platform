using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.ApiCalls;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ApiCallService : CrudService<ApiCall>, IApiCallService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ApiCallService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<ApiCall>> GetApiCalls(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = DbContext.Set<ApiCall>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower) || (item.Url != null && item.Url.ToLower().Contains(lower)));
            }
            return await query
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<ApiCall?> GetApiCallById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ApiCall>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<ApiCall> CreateApiCall(CreateApiCallRequest request, CancellationToken cancellationToken = default)
        {
            ApiCall apiCall = new(
                request.Name,
                NormalizeHttpMethod(request.Method),
                request.Url,
                request.Description,
                request.HeadersTemplate,
                request.BodyTemplate);

            bool success = await Insert(cancellationToken, apiCall);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return apiCall;
        }

        public async Task<ApiCall> UpdateApiCall(long id, UpdateApiCallRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            ApiCall? apiCall = await DbContext.Set<ApiCall>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (apiCall is null)
            {
                throw new InvalidOperationException("apiCall.notFound");
            }

            apiCall.Update(
                request.Name,
                NormalizeHttpMethod(request.Method),
                request.Url,
                request.Description,
                request.HeadersTemplate,
                request.BodyTemplate);

            ApiCall? result = await Update(apiCall, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private HttpMethodType NormalizeHttpMethod(int value)
        {
            int normalizedValue = Enum.IsDefined(typeof(HttpMethodType), value)
                ? value
                : value + 1;

            if (!Enum.IsDefined(typeof(HttpMethodType), normalizedValue))
            {
                throw new InvalidOperationException("apiCall.method.invalid");
            }

            return (HttpMethodType)normalizedValue;
        }

        public override async Task<ApiCall?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            ApiCall? apiCall = await DbContext.Set<ApiCall>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (apiCall is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["apiCall.notFound"]));
                return null;
            }

            return await Delete([apiCall], cancellationToken) ? apiCall : null;
        }
    }
}
