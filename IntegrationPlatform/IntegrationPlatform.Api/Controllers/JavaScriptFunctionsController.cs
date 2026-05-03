using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.JavaScriptFunctions;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class JavaScriptFunctionsController : ApiControllerBase
    {
        private readonly IJavaScriptFunctionService javaScriptFunctionService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }

        public JavaScriptFunctionsController(IJavaScriptFunctionService javaScriptFunctionService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.javaScriptFunctionService = javaScriptFunctionService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as funções JavaScript cadastradas na plataforma.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<JavaScriptFunction> result = await javaScriptFunctionService.GetJavaScriptFunctions(request, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("Permite consultar os detalhes de uma função JavaScript específica.")]
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

        [RequireAccess("Permite cadastrar uma nova função JavaScript reutilizável.")]
        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite atualizar uma função JavaScript cadastrada na plataforma.")]
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

        [RequireAccess("Permite excluir uma função JavaScript cadastrada.")]
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
