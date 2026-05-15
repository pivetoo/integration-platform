using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.IntegrationCategories;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class IntegrationCategoriesController : ApiControllerBase
    {
        private readonly IIntegrationCategoryService integrationCategoryService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }

        public IntegrationCategoriesController(IIntegrationCategoryService integrationCategoryService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.integrationCategoryService = integrationCategoryService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as categorias de integração cadastradas na plataforma.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            PagedResult<IntegrationCategory> result = await integrationCategoryService.GetIntegrationCategories(request, search, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("Permite consultar os detalhes de uma categoria de integração específica.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            IntegrationCategory? entity = await integrationCategoryService.GetIntegrationCategoryById(id, cancellationToken);
            return entity is null ? Http404(Localizer["record.notFound"]) : Http200(entity);
        }

        [RequireAccess("Permite cadastrar uma nova categoria de integração.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateIntegrationCategoryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationCategory category = await integrationCategoryService.CreateIntegrationCategory(request, cancellationToken);
            return Http201(category, Localizer["integration.category.created"]);
        }

        [RequireAccess("Permite atualizar os dados de uma categoria de integração cadastrada.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateIntegrationCategoryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationCategory category = await integrationCategoryService.UpdateIntegrationCategory(id, request, cancellationToken);
            return Http200(category, Localizer["integration.category.updated"]);
        }

        [RequireAccess]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<IntegrationCategory> categories = await integrationCategoryService.GetActiveIntegrationCategories(cancellationToken);
            return Http200(categories);
        }

        [RequireAccess("Permite excluir uma categoria de integração cadastrada.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            IntegrationCategory? category = await integrationCategoryService.Delete(id, cancellationToken);
            if (category is null)
            {
                return Http404(integrationCategoryService.GetErrorMessages());
            }

            return Http200(category, Localizer["integration.category.deleted"]);
        }
    }
}
