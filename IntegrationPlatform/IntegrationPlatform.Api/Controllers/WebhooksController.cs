using Archon.Api.Controllers;
using Archon.Application.MultiTenancy;
using Archon.Infrastructure.MultiTenancy;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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

        // Webhook por convencao: roda o pipeline "{integration}-webhook" do primeiro connector ativo.
        [HttpPost("{tenantId}/{integrationIdentifier}")]
        public async Task<IActionResult> Receive(string tenantId, string integrationIdentifier, CancellationToken cancellationToken)
        {
            if (!await ResolveTenant(tenantId, cancellationToken))
            {
                return Http404("tenant.notFound");
            }

            string rawBody = await ReadBody(cancellationToken);
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

        // Webhook com pipeline explicito na rota (para integracoes com mais de um pipeline de webhook).
        [HttpPost("{tenantId}/{integrationIdentifier}/{pipelineIdentifier}")]
        public async Task<IActionResult> ReceiveForPipeline(string tenantId, string integrationIdentifier, string pipelineIdentifier, CancellationToken cancellationToken)
        {
            if (!await ResolveTenant(tenantId, cancellationToken))
            {
                return Http404("tenant.notFound");
            }

            string rawBody = await ReadBody(cancellationToken);
            IExecutionEngineService executionEngineService = HttpContext.RequestServices.GetRequiredService<IExecutionEngineService>();

            try
            {
                Execution execution = await executionEngineService.ExecutePipelineByIdentifier(
                    integrationIdentifier,
                    pipelineIdentifier,
                    ParseBody(rawBody),
                    ExecutionType.Webhook,
                    cancellationToken);

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

        // Verificacao estilo Meta/WhatsApp: echo do hub.challenge, valido nas duas formas de rota.
        [HttpGet("{tenantId}/{integrationIdentifier}")]
        [HttpGet("{tenantId}/{integrationIdentifier}/{pipelineIdentifier}")]
        public IActionResult Challenge([FromQuery(Name = "hub.challenge")] string? hubChallenge)
        {
            if (string.IsNullOrWhiteSpace(hubChallenge))
            {
                return Http400("hub.challenge.required");
            }

            return Ok(hubChallenge);
        }

        private async Task<bool> ResolveTenant(string tenantId, CancellationToken cancellationToken)
        {
            TenantInfo? tenant = await tenantResolver.ResolveAsync(tenantId, cancellationToken);
            if (tenant is null)
            {
                return false;
            }

            ITenantContext tenantContext = HttpContext.RequestServices.GetRequiredService<ITenantContext>();
            if (tenantContext is MultiTenantContext multiTenantContext)
            {
                multiTenantContext.SetTenant(tenant);
            }

            return true;
        }

        private async Task<string> ReadBody(CancellationToken cancellationToken)
        {
            using StreamReader reader = new(Request.Body);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        private static Dictionary<string, object> ParseBody(string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return [];
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(rawBody);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(rawBody) ?? [];
                }
            }
            catch (JsonException)
            {
            }

            return new Dictionary<string, object> { ["payload"] = rawBody };
        }
    }
}
