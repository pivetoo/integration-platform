using Archon.Api.Controllers;
using Archon.Application.MultiTenancy;
using Archon.Infrastructure.MultiTenancy;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationPlatform.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/webhooks")]
    public sealed class WebhooksController : ApiControllerBase
    {
        private readonly ITenantResolver tenantResolver;

        public WebhooksController(ITenantResolver tenantResolver)
        {
            this.tenantResolver = tenantResolver;
        }

        [HttpPost("{tenantId}/{integrationIdentifier}")]
        public async Task<IActionResult> Receive(string tenantId, string integrationIdentifier, CancellationToken cancellationToken)
        {
            TenantInfo? tenant = await tenantResolver.ResolveAsync(tenantId, cancellationToken);

            if (tenant is null)
            {
                return Http404("tenant.notFound");
            }

            SetTenant(tenant);

            string rawBody;
            using (StreamReader reader = new(Request.Body))
            {
                rawBody = await reader.ReadToEndAsync(cancellationToken);
            }

            IExecutionEngineService executionEngineService = HttpContext.RequestServices.GetRequiredService<IExecutionEngineService>();

            try
            {
                Execution execution = await executionEngineService.ExecuteWebhookByIntegration(
                    integrationIdentifier,
                    rawBody,
                    cancellationToken);

                return Http200(new
                {
                    executionId = execution.Id,
                    status = execution.Status.ToString(),
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Http404(Localizer[ex.Message]);
            }
            catch (InvalidOperationException ex)
            {
                return Http400(Localizer[ex.Message]);
            }
        }

        private void SetTenant(TenantInfo tenant)
        {
            ITenantContext tenantContext = HttpContext.RequestServices.GetRequiredService<ITenantContext>();
            if (tenantContext is MultiTenantContext multiTenantContext)
            {
                multiTenantContext.SetTenant(tenant);
            }
        }
    }
}
