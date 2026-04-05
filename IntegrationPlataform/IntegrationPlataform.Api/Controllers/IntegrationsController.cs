using Archon.Api.Attributes;
using Archon.Api.Controllers;
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
    public sealed class IntegrationsController : ReadOnlyController<Integration>
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

            return integration is null ? Http404("Record not found.") : Http200(integration);
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
            return Http201(integration, "Integration created successfully.");
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
            return Http200(integration, "Integration updated successfully.");
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            Integration? integration = await integrationService.Delete(id, cancellationToken);
            if (integration is null)
            {
                return Http404(integrationService.GetErrorMessages());
            }

            return Http200(integration, "Integration deleted successfully.");
        }
    }
}
