using Archon.Api.Attributes;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.Integrations;
using IntegrationPlataform.Application.Requests.Integrations;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class IntegrationsController : IntegrationPlataformReadOnlyController<Integration>
    {
        private readonly IIntegrationService integrationService;

        public IntegrationsController(DbContext dbContext, IIntegrationService integrationService)
            : base(dbContext)
        {
            this.integrationService = integrationService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await DbContext.Set<Integration>()
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .Select(IntegrationContract.Projection)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var integration = await DbContext.Set<Integration>()
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Select(IntegrationContract.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            return integration is null ? Http404(Localizer["integration.notFound"]) : Http200(integration);
        }

        [RequireAccess]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var integrations = await DbContext.Set<Integration>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .Select(IntegrationContract.Projection)
                .ToListAsync(cancellationToken);

            return Http200(integrations);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateIntegrationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Integration integration = await integrationService.CreateIntegration(request, cancellationToken);

            IntegrationContract? contract = await DbContext.Set<Integration>()
                .AsNoTracking()
                .Where(item => item.Id == integration.Id)
                .Select(IntegrationContract.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            return Http201(contract ?? IntegrationContract.Projection.Compile()(integration), Localizer["integration.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateIntegrationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Integration integration = await integrationService.UpdateIntegration(id, request, cancellationToken);

            IntegrationContract? contract = await DbContext.Set<Integration>()
                .AsNoTracking()
                .Where(item => item.Id == integration.Id)
                .Select(IntegrationContract.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            return Http200(contract ?? IntegrationContract.Projection.Compile()(integration), Localizer["integration.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            IntegrationContract? contract = await DbContext.Set<Integration>()
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Select(IntegrationContract.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            Integration? integration = await integrationService.Delete(id, cancellationToken);
            if (integration is null)
            {
                return Http404(integrationService.GetErrorMessages());
            }

            return Http200(contract ?? IntegrationContract.Projection.Compile()(integration), Localizer["integration.deleted"]);
        }
    }
}
