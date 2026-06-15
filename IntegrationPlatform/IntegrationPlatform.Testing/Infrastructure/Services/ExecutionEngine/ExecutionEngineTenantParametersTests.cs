using Archon.Application.Integrations;
using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;

namespace IntegrationPlatform.Testing.Infrastructure.Services.ExecutionEngine
{
    [TestFixture]
    public class ExecutionEngineTenantParametersTests
    {
        [Test]
        public void ApplyTenantParameters_AddsParametersAndFlagsSecrets()
        {
            PipelineExecutionContext context = new();
            Integration tenantConfig = new()
            {
                Name = "agency-campaign",
                BaseUrl = "https://agencias.mainstay.com.br",
                Parameters = new[]
                {
                    new IntegrationParameter { Key = "CallbackSecret", Value = "s3cr3t", IsSecret = true },
                    new IntegrationParameter { Key = "callbackBaseUrl", Value = "https://x", IsSecret = false }
                }
            };

            ExecutionEngineService.ApplyTenantParameters(context, tenantConfig);

            Assert.That(context.ConnectorAttributes["CallbackSecret"], Is.EqualTo("s3cr3t"));
            Assert.That(context.ConnectorAttributes["callbackBaseUrl"], Is.EqualTo("https://x"));
            Assert.That(context.SensitiveAttributeFields, Does.Contain("CallbackSecret"));
            Assert.That(context.SensitiveAttributeFields, Does.Not.Contain("callbackBaseUrl"));
        }

        [Test]
        public void ApplyTenantParameters_DoesNotOverrideExistingConnectorAttribute()
        {
            PipelineExecutionContext context = new();
            context.ConnectorAttributes["webhook_secret"] = "connector-value";
            Integration tenantConfig = new()
            {
                Name = "agency-campaign",
                Parameters = new[]
                {
                    new IntegrationParameter { Key = "webhook_secret", Value = "tenant-value", IsSecret = true }
                }
            };

            ExecutionEngineService.ApplyTenantParameters(context, tenantConfig);

            Assert.That(context.ConnectorAttributes["webhook_secret"], Is.EqualTo("connector-value"));
            Assert.That(context.SensitiveAttributeFields, Does.Not.Contain("webhook_secret"));
        }

        [Test]
        public void ApplyTenantParameters_NullConfig_IsNoOp()
        {
            PipelineExecutionContext context = new();

            ExecutionEngineService.ApplyTenantParameters(context, null);

            Assert.That(context.ConnectorAttributes, Is.Empty);
        }
    }
}
