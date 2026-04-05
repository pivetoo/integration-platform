using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.JavaScriptFunctions;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class JavaScriptFunctionsController : ApiControllerBase
    {
        private readonly IJavaScriptFunctionService javaScriptFunctionService;
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }

        public JavaScriptFunctionsController(IJavaScriptFunctionService javaScriptFunctionService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.javaScriptFunctionService = javaScriptFunctionService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<JavaScriptFunction> result = await javaScriptFunctionService.GetJavaScriptFunctions(request, cancellationToken);
            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            JavaScriptFunction? entity = await javaScriptFunctionService.GetJavaScriptFunctionById(id, cancellationToken);
            return entity is null ? Http404(Localizer["record.notFound"]) : Http200(entity);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateJavaScriptFunctionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            JavaScriptFunction function = await javaScriptFunctionService.CreateJavaScriptFunction(request, cancellationToken);
            return Http201(function, Localizer["javaScriptFunction.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateJavaScriptFunctionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            JavaScriptFunction function = await javaScriptFunctionService.UpdateJavaScriptFunction(id, request, cancellationToken);
            return Http200(function, Localizer["javaScriptFunction.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            JavaScriptFunction? function = await javaScriptFunctionService.Delete(id, cancellationToken);
            if (function is null)
            {
                return Http404(javaScriptFunctionService.GetErrorMessages());
            }

            return Http200(function, Localizer["javaScriptFunction.deleted"]);
        }
    }
}
