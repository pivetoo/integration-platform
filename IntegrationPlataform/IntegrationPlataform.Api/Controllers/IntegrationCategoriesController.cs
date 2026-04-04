using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.IntegrationCategories;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class IntegrationCategoriesController : ReadOnlyController<IntegrationCategory>
    {
        private readonly IIntegrationCategoryService integrationCategoryService;

        public IntegrationCategoriesController(DbContext dbContext, IIntegrationCategoryService integrationCategoryService) : base(dbContext)
        {
            this.integrationCategoryService = integrationCategoryService;
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
        public async Task<IActionResult> Create([FromBody] CreateIntegrationCategoryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationCategory category = await integrationCategoryService.CreateIntegrationCategory(request, cancellationToken);
            return Http201(category, "Integration category created successfully.");
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateIntegrationCategoryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationCategory category = await integrationCategoryService.UpdateIntegrationCategory(id, request, cancellationToken);
            return Http200(category, "Integration category updated successfully.");
        }

        [RequireAccess]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<IntegrationCategory> categories = await DbContext.Set<IntegrationCategory>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);

            return Http200(categories);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            IntegrationCategory? category = await integrationCategoryService.Delete(id, cancellationToken);
            if (category is null)
            {
                return Http404(integrationCategoryService.GetErrorMessages());
            }

            return Http200(category, "Integration category deleted successfully.");
        }
    }
}
