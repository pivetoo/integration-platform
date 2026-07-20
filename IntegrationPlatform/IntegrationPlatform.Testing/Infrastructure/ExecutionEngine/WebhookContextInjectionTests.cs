using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;
using System.Text.Json;

namespace IntegrationPlatform.Testing.Infrastructure.ExecutionEngine
{
    [TestFixture]
    public sealed class WebhookContextInjectionTests
    {
        [Test]
        public void InjectWebhookContext_should_add_context_preserving_existing_fields()
        {
            string result = ExecutionEngineService.InjectWebhookContext("{\"uuid\":\"abc\",\"type_post\":\"4\"}", "token-123");

            Dictionary<string, JsonElement> fields = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result)!;
            fields["uuid"].GetString().Should().Be("abc");
            fields["type_post"].GetString().Should().Be("4");
            fields["webhookContext"].GetString().Should().Be("token-123");
        }

        [Test]
        public void InjectWebhookContext_should_add_context_to_wrapped_raw_payload()
        {
            string result = ExecutionEngineService.InjectWebhookContext("{\"raw\":\"corpo-nao-json\"}", "token-456");

            Dictionary<string, JsonElement> fields = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result)!;
            fields["raw"].GetString().Should().Be("corpo-nao-json");
            fields["webhookContext"].GetString().Should().Be("token-456");
        }
    }
}
