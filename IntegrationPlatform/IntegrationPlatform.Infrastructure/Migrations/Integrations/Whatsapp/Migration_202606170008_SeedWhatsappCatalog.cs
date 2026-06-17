using FluentMigrator;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Whatsapp
{
    // Catalogo do modulo Whatsapp: categoria 'whatsapp' + contract 'whatsapp.send' (com callback).
    // Compartilhados pelas integracoes de whatsapp (cada uma na sua pasta). Roda antes delas (versao menor).
    [Migration(202606170008)]
    public sealed class Migration_202606170008_SeedWhatsappCatalog : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("whatsapp", "WhatsApp", "Provedores de mensageria WhatsApp.");

            SeedContract(
                identifier: "whatsapp.send",
                name: "Envio de mensagem WhatsApp",
                description: "Envia mensagem via WhatsApp.",
                categoryIdentifier: "whatsapp",
                inputSchema: """
                {
                  "to": "string (E.164, obrigatorio)",
                  "channel": "whatsapp | sms (obrigatorio)",
                  "template": "{name, language, variables{}}?",
                  "body": "string?",
                  "attachments": "[{type: image|document|video, url}]?"
                }
                """,
                outputSchema: """
                {
                  "providerMessageId": "string",
                  "status": "queued | sent | rejected"
                }
                """,
                hasCallback: true,
                callbackSchema: """
                {
                  "providerMessageId": "string",
                  "status": "sent | delivered | read | failed",
                  "error": "{code, message}?"
                }
                """);
        }

        public override void Down()
        {
            // Catalogo de convergencia; Down nao reverte (integracoes/connectors referenciam estes registros).
        }
    }
}
