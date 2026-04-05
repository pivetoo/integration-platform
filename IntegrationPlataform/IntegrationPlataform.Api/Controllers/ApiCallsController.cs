using Archon.Api.Attributes;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.ApiCalls;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ApiCallsController : IntegrationPlataformReadOnlyController<ApiCall>
    {
        private readonly IApiCallService apiCallService;

        public ApiCallsController(DbContext dbContext, IApiCallService apiCallService) : base(dbContext)
        {
            this.apiCallService = apiCallService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            return await base.Get(request, cancellationToken);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            return await base.GetById(id, cancellationToken);
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
            return Http201(apiCall, Localizer["apiCall.created"]);
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
            return Http200(apiCall, Localizer["apiCall.updated"]);
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

            return Http200(apiCall, Localizer["apiCall.deleted"]);
        }
    }
}
