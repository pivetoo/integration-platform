using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Requests.ApiCalls;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ApiCallService : CrudService<ApiCall>, IApiCallService
    {
        public ApiCallService(DbContext dbContext) : base(dbContext)
        {
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
                throw new InvalidOperationException("Route id does not match body id.");
            }

            ApiCall? apiCall = await DbContext.Set<ApiCall>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (apiCall is null)
            {
                throw new InvalidOperationException("Api call not found.");
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

        private static HttpMethodType NormalizeHttpMethod(int value)
        {
            int normalizedValue = Enum.IsDefined(typeof(HttpMethodType), value)
                ? value
                : value + 1;

            if (!Enum.IsDefined(typeof(HttpMethodType), normalizedValue))
            {
                throw new InvalidOperationException("Invalid http method.");
            }

            return (HttpMethodType)normalizedValue;
        }
    }
}
