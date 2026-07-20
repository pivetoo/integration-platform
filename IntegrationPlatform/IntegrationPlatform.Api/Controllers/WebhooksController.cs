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

        // O segmento opcional {context} carrega correlacao para provedores que nao ecoam identificador
        // proprio no payload (ex.: postback da D4Sign); chega ao pipeline como payload.webhookContext.
        [HttpPost("{tenantId}/{integrationIdentifier}/{context?}")]
        public async Task<IActionResult> Receive(string tenantId, string integrationIdentifier, string? context, CancellationToken cancellationToken)
        {
            if (!await webhookReceiverService.ResolveTenantAsync(tenantId, cancellationToken))
            {
                return Http404("tenant.notFound");
            }

            string rawBody;
            if (Request.HasFormContentType)
            {
                // Provedores como a D4Sign enviam o postback em form-data; normaliza para JSON
                // para o pipeline consumir os campos como payload.*.
                IFormCollection form = await Request.ReadFormAsync(cancellationToken);
                Dictionary<string, string> fields = form.Keys.ToDictionary(key => key, key => form[key].ToString());
                rawBody = System.Text.Json.JsonSerializer.Serialize(fields);
            }
            else
            {
                rawBody = await webhookReceiverService.ReadBodyAsync(Request.Body, cancellationToken);
            }

            IExecutionEngineService executionEngineService = HttpContext.RequestServices.GetRequiredService<IExecutionEngineService>();

            try
            {
                Execution execution = await executionEngineService.ExecuteWebhookByIntegration(integrationIdentifier, rawBody, context, cancellationToken);
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
