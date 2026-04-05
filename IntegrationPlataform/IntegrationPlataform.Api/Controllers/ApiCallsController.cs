using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.ApiCalls;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.ApiCalls;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ApiCallsController : ApiControllerBase
    {
        private readonly IApiCallService apiCallService;
        private static readonly Func<ApiCall, ApiCallContract> MapApiCall = ApiCallContract.Projection.Compile();
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }

        public ApiCallsController(IApiCallService apiCallService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.apiCallService = apiCallService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<ApiCall> result = await apiCallService.GetApiCalls(request, cancellationToken);
            return Http200(new PagedResult<ApiCallContract>
            {
                Items = result.Items.Select(MapApiCall).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            ApiCall? apiCall = await apiCallService.GetApiCallById(id, cancellationToken);
            return apiCall is null ? Http404(Localizer["apiCall.notFound"]) : Http200(MapApiCall(apiCall));
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateApiCallRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ApiCall apiCall = await apiCallService.CreateApiCall(request, cancellationToken);
            return Http201(MapApiCall(apiCall), Localizer["apiCall.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateApiCallRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ApiCall apiCall = await apiCallService.UpdateApiCall(id, request, cancellationToken);
            return Http200(MapApiCall(apiCall), Localizer["apiCall.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            ApiCall? apiCall = await apiCallService.Delete(id, cancellationToken);
            if (apiCall is null)
            {
                return Http404(apiCallService.GetErrorMessages());
            }

            return Http200(MapApiCall(apiCall), Localizer["apiCall.deleted"]);
        }
    }
}
