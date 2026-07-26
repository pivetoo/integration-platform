using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations
{
    // Regua de Cobranca do Mainstay: os contratos de email/whatsapp passam a prever (a) a chave de correlacao
    // do disparo e (b) o desfecho do envio, para o consumidor sair do "achei que enviei".
    //
    // O que muda aqui e SO o catalogo (schemas + hascallback + includeoutputincallback), que vale para
    // qualquer provedor. O eco do correlationKey e a extracao do providerMessageId sao especificos de cada
    // pipeline de provedor e ficam para quando houver um provedor configurado e testavel — hoje nao ha
    // NENHUM connector de email/whatsapp em nenhum tenant.
    //
    // Atencao: 'whatsapp.enviar' ja declarava 'template' no inputSchema, mas nenhuma pipeline usa; as
    // pipelines '*-enviar-template' existem e estao com servicecontractid NULL (nao roteiam pelo contrato).
    //
    // SeedContract so INSERE (WHERE NOT EXISTS); atualizar contrato existente exige UPDATE explicito.
    [Migration(202607260002)]
    public sealed class Migration_202607260002_ReminderDeliveryCallbackContracts : IntegrationSeedMigration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE servicecontract SET
                    inputschema = '{
                  "to": "string[] (obrigatorio)",
                  "cc": "string[]?",
                  "bcc": "string[]?",
                  "subject": "string (obrigatorio)",
                  "body": "string (obrigatorio)",
                  "isHtml": "bool",
                  "attachments": "[{filename, contentType, contentBase64 | url}]?",
                  "replyTo": "string?",
                  "from": "{email, name}?",
                  "correlationKey": "string? (eco no callback; correlaciona o desfecho no consumidor)"
                }',
                    callbackschema = '{
                  "correlationKey": "string",
                  "eventType": "delivered | bounced | rejected | failed",
                  "providerMessageId": "string?",
                  "error": "string?"
                }',
                    hascallback = true,
                    includeoutputincallback = true,
                    updatedat = now()
                WHERE identifier = 'email.enviar';

                UPDATE servicecontract SET
                    inputschema = '{
                  "to": "string (E.164, obrigatorio)",
                  "channel": "whatsapp | sms (obrigatorio)",
                  "template": "{name, language, variables{}}?",
                  "body": "string?",
                  "attachments": "[{type: image|document|video, url}]?",
                  "correlationKey": "string? (eco no callback; correlaciona o desfecho no consumidor)"
                }',
                    callbackschema = '{
                  "correlationKey": "string",
                  "eventType": "delivered | bounced | rejected | failed",
                  "providerMessageId": "string?",
                  "error": "string?"
                }',
                    hascallback = true,
                    includeoutputincallback = true,
                    updatedat = now()
                WHERE identifier = 'whatsapp.enviar';
                """);
        }

        public override void Down()
        {
            Execute.Sql("""
                UPDATE servicecontract SET hascallback = false, callbackschema = NULL, includeoutputincallback = false, updatedat = now()
                WHERE identifier IN ('email.enviar', 'whatsapp.enviar');
                """);
        }
    }
}
