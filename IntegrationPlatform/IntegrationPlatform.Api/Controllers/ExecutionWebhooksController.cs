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
    [Route("api/executions/webhook")]
    public sealed class ExecutionWebhooksController : ApiControllerBase
    {
        private readonly ITenantResolver tenantResolver;

        public ExecutionWebhooksController(ITenantResolver tenantResolver)
        {
            this.tenantResolver = tenantResolver;
        }

        [HttpPost("{tenantId}/{integrationIdentifier}/{pipelineIdentifier}")]
        public async Task<IActionResult> Receive(
            string tenantId,
            string integrationIdentifier,
            string pipelineIdentifier,
            CancellationToken cancellationToken)
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
                Execution execution = await executionEngineService.ExecutePipelineByIdentifier(
                    integrationIdentifier,
                    pipelineIdentifier,
                    ParseBody(rawBody),
                    ExecutionType.Webhook,
                    cancellationToken);

                return Http200(new
                {
                    executionId = execution.Id,
                    status = execution.Status.ToString(),
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Http404(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Http400(ex.Message);
            }
        }

        [HttpGet("{tenantId}/{integrationIdentifier}/{pipelineIdentifier}")]
        public IActionResult Challenge([FromQuery(Name = "hub.challenge")] string? hubChallenge)
        {
            if (string.IsNullOrWhiteSpace(hubChallenge))
            {
                return Http400("hub.challenge.required");
            }

            return Ok(hubChallenge);
        }

        private void SetTenant(TenantInfo tenant)
        {
            ITenantContext tenantContext = HttpContext.RequestServices.GetRequiredService<ITenantContext>();
            if (tenantContext is MultiTenantContext multiTenantContext)
            {
                multiTenantContext.SetTenant(tenant);
            }
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
            catch { }

            return new Dictionary<string, object> { ["payload"] = rawBody };
        }
    }
}
