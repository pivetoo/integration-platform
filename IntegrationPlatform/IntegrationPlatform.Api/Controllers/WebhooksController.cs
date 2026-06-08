using Archon.Api.Controllers;
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
        private readonly IWebhookReceiverService webhookReceiverService;

        public WebhooksController(IWebhookReceiverService webhookReceiverService)
        {
            this.webhookReceiverService = webhookReceiverService;
        }

        [HttpPost("{tenantId}/{integrationIdentifier}")]
        public async Task<IActionResult> Receive(string tenantId, string integrationIdentifier, CancellationToken cancellationToken)
        {
            if (!await webhookReceiverService.ResolveTenantAsync(tenantId, cancellationToken))
            {
                return Http404("tenant.notFound");
            }

            string rawBody = await webhookReceiverService.ReadBodyAsync(Request.Body, cancellationToken);
            IExecutionEngineService executionEngineService = HttpContext.RequestServices.GetRequiredService<IExecutionEngineService>();

            try
            {
                Execution execution = await executionEngineService.ExecuteWebhookByIntegration(integrationIdentifier, rawBody, cancellationToken);
                return Http200(new { executionId = execution.Id, status = execution.Status.ToString() });
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
    }
}
