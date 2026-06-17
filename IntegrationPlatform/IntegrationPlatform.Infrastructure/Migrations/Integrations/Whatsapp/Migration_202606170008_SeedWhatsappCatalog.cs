using FluentMigrator;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Whatsapp
{
    // Catalogo do modulo Whatsapp: categoria 'whatsapp' + contract 'whatsapp.enviar' (fire-and-forget; sem callback por enquanto).
    // Compartilhados pelas integracoes de whatsapp (cada uma na sua pasta). Roda antes delas (versao menor).
    [Migration(202606170008)]
    public sealed class Migration_202606170008_SeedWhatsappCatalog : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("whatsapp", "WhatsApp", "Provedores de mensageria WhatsApp.");

            SeedContract(
                identifier: "whatsapp.enviar",
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
                hasCallback: false,
                callbackSchema: null);
        }

        public override void Down()
        {
            // Catalogo de convergencia; Down nao reverte (integracoes/connectors referenciam estes registros).
        }
    }
}
