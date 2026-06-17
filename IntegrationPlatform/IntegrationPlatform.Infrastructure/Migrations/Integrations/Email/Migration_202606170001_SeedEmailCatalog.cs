using FluentMigrator;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Email
{
    // Catalogo do modulo Email: categoria 'email' + contract 'email.enviar'.
    // Compartilhados pelas integracoes de email (cada uma na sua pasta). Roda antes delas (versao menor).
    [Migration(202606170001)]
    public sealed class Migration_202606170001_SeedEmailCatalog : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("email", "Email", "Provedores de envio de e-mail transacional.");

            SeedContract(
                identifier: "email.enviar",
                name: "Envio de email",
                description: "Envia um e-mail transacional para um ou mais destinatários.",
                categoryIdentifier: "email",
                inputSchema: """
                {
                  "to": "string[] (obrigatorio)",
                  "cc": "string[]?",
                  "bcc": "string[]?",
                  "subject": "string (obrigatorio)",
                  "body": "string (obrigatorio)",
                  "isHtml": "bool",
                  "attachments": "[{filename, contentType, contentBase64 | url}]?",
                  "replyTo": "string?",
                  "from": "{email, name}?"
                }
                """,
                outputSchema: """
                {
                  "providerMessageId": "string",
                  "status": "accepted | rejected",
                  "error": "{code, message}?"
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
