using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.JavaScriptFunctions;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class JavaScriptFunctionsController : ReadOnlyController<JavaScriptFunction>
    {
        private readonly IJavaScriptFunctionService javaScriptFunctionService;

        public JavaScriptFunctionsController(DbContext dbContext, IJavaScriptFunctionService javaScriptFunctionService) : base(dbContext)
        {
            this.javaScriptFunctionService = javaScriptFunctionService;
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
        public async Task<IActionResult> Create([FromBody] CreateJavaScriptFunctionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            JavaScriptFunction function = await javaScriptFunctionService.CreateJavaScriptFunction(request, cancellationToken);
            return Http201(function, "JavaScript function created successfully.");
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
            return Http200(function, "JavaScript function updated successfully.");
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

            return Http200(function, "JavaScript function deleted successfully.");
        }
    }
}
