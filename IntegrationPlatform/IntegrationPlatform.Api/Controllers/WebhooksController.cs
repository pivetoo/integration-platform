using Archon.Api.Controllers;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.IO;

namespace IntegrationPlatform.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/webhooks")]
    public sealed class WebhooksController : ApiControllerBase
    {
        private readonly IExecutionEngineService executionEngineService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }

        public WebhooksController(IExecutionEngineService executionEngineService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.executionEngineService = executionEngineService;
            Localizer = localizer;
        }

        [HttpPost("{token}")]
        public async Task<IActionResult> Receive(string token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Http400(Localizer["webhook.token.required"]);
            }

            string rawBody;
            using (StreamReader reader = new(Request.Body))
            {
                rawBody = await reader.ReadToEndAsync(cancellationToken);
            }

            try
            {
                Execution execution = await executionEngineService.ExecuteWebhook(token, rawBody, cancellationToken);
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
    }
}
