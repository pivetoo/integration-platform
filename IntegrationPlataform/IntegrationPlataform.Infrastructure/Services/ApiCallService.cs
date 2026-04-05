using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.ApiCalls;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ApiCallService : CrudService<ApiCall>, IApiCallService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public ApiCallService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
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
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            ApiCall? apiCall = await DbContext.Set<ApiCall>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (apiCall is null)
            {
                throw new InvalidOperationException(Localizer["apiCall.notFound"]);
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
                throw new InvalidOperationException(Localizer["apiCall.method.invalid"]);
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
