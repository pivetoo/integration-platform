using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200001)]
    public sealed class Migration_202605200001_AddServiceContractTables : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS servicecontract (
                    id BIGSERIAL PRIMARY KEY,
                    identifier VARCHAR(120) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(500) NULL,
                    integrationcategoryid BIGINT NOT NULL REFERENCES integrationcategory(id),
                    inputschema TEXT NULL,
                    outputschema TEXT NULL,
                    hascallback BOOLEAN NOT NULL DEFAULT false,
                    callbackschema TEXT NULL,
                    isactive BOOLEAN NOT NULL DEFAULT true,
                    issystem BOOLEAN NOT NULL DEFAULT false,
                    createdat TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                    updatedat TIMESTAMP NULL
                );
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_servicecontract_identifier ON servicecontract(identifier);");
            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_servicecontract_integrationcategoryid ON servicecontract(integrationcategoryid);");

            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS integrationservicecontract (
                    id BIGSERIAL PRIMARY KEY,
                    integrationid BIGINT NOT NULL REFERENCES integration(id),
                    servicecontractid BIGINT NOT NULL REFERENCES servicecontract(id),
                    isactive BOOLEAN NOT NULL DEFAULT true,
                    createdat TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                    updatedat TIMESTAMP NULL
                );
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_integrationservicecontract_pair ON integrationservicecontract(integrationid, servicecontractid);");
            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_integrationservicecontract_integrationid ON integrationservicecontract(integrationid);");
            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_integrationservicecontract_servicecontractid ON integrationservicecontract(servicecontractid);");

            Execute.Sql(@"
                ALTER TABLE pipeline
                    ADD COLUMN IF NOT EXISTS servicecontractid BIGINT NULL REFERENCES servicecontract(id);
            ");

            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_pipeline_servicecontractid ON pipeline(servicecontractid);");

            Execute.Sql(@"
                INSERT INTO integrationcategory (identifier, name, description, isactive, issystem, createdat, updatedat)
                SELECT 'messaging', 'Mensageria', 'Provedores de envio de mensagens (WhatsApp, SMS).', true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = 'messaging');
            ");

            SeedServiceContract(
                identifier: "email.send",
                name: "Envio de email",
                description: "Envia um email transacional para um ou mais destinatarios.",
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

            SeedServiceContract(
                identifier: "messaging.send",
                name: "Envio de mensagem",
                description: "Envia mensagem por canais de mensageria (WhatsApp, SMS).",
                categoryIdentifier: "messaging",
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

            SeedServiceContract(
                identifier: "signature.envelope.create",
                name: "Criar envelope de assinatura",
                description: "Cria um envelope para coleta de assinaturas digitais.",
                categoryIdentifier: "digital-signature",
                inputSchema: """
                {
                  "document": "{filename, contentType, contentBase64 | url} (obrigatorio)",
                  "signers": "[{name, email, phone?, role, signatureFields?}] (obrigatorio)",
                  "title": "string (obrigatorio)",
                  "message": "string?",
                  "deadline": "ISO date?",
                  "reminderDays": "number?"
                }
                """,
                outputSchema: """
                {
                  "envelopeId": "string",
                  "signers": "[{signerId, status, signUrl?}]"
                }
                """,
                hasCallback: true,
                callbackSchema: """
                {
                  "envelopeId": "string",
                  "status": "pending | partially_signed | completed | rejected | expired",
                  "signedDocumentUrl": "string?",
                  "signers": "[{signerId, status, signedAt?}]"
                }
                """);

            SeedServiceContract(
                identifier: "banking.account.sync",
                name: "Sincronizar conta bancaria",
                description: "Dispara sincronizacao de saldo e transacoes de uma conta bancaria.",
                categoryIdentifier: "banking",
                inputSchema: """
                {
                  "accountReference": "string (obrigatorio)",
                  "fromDate": "ISO date (obrigatorio)",
                  "toDate": "ISO date?",
                  "includeBalance": "bool"
                }
                """,
                outputSchema: """
                {
                  "syncJobId": "string"
                }
                """,
                hasCallback: true,
                callbackSchema: """
                {
                  "syncJobId": "string",
                  "status": "completed | failed",
                  "balance": "{currency, available, blocked?}?",
                  "transactions": "[{externalId, date, amount, description, type}]?",
                  "error": "{code, message}?"
                }
                """);
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX IF EXISTS ix_pipeline_servicecontractid;");
            Execute.Sql("ALTER TABLE pipeline DROP COLUMN IF EXISTS servicecontractid;");

            Execute.Sql("DROP INDEX IF EXISTS ix_integrationservicecontract_servicecontractid;");
            Execute.Sql("DROP INDEX IF EXISTS ix_integrationservicecontract_integrationid;");
            Execute.Sql("DROP INDEX IF EXISTS ux_integrationservicecontract_pair;");
            Execute.Sql("DROP TABLE IF EXISTS integrationservicecontract;");

            Execute.Sql("DROP INDEX IF EXISTS ix_servicecontract_integrationcategoryid;");
            Execute.Sql("DROP INDEX IF EXISTS ux_servicecontract_identifier;");
            Execute.Sql("DROP TABLE IF EXISTS servicecontract;");

            Execute.Sql("DELETE FROM integrationcategory WHERE identifier = 'messaging' AND issystem = true;");
        }

        private void SeedServiceContract(string identifier, string name, string description, string categoryIdentifier, string inputSchema, string outputSchema, bool hasCallback, string? callbackSchema)
        {
            string escapedDescription = description.Replace("'", "''");
            string escapedInput = inputSchema.Replace("'", "''");
            string escapedOutput = outputSchema.Replace("'", "''");
            string escapedCallback = callbackSchema?.Replace("'", "''") ?? string.Empty;
            string callbackSql = callbackSchema is null ? "NULL" : $"'{escapedCallback}'";

            Execute.Sql($@"
                INSERT INTO servicecontract (identifier, name, description, integrationcategoryid, inputschema, outputschema, hascallback, callbackschema, isactive, issystem, createdat, updatedat)
                SELECT '{identifier}', '{name}', '{escapedDescription}', (SELECT id FROM integrationcategory WHERE identifier = '{categoryIdentifier}' LIMIT 1), '{escapedInput}', '{escapedOutput}', {hasCallback.ToString().ToLower()}, {callbackSql}, true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM servicecontract WHERE identifier = '{identifier}');
            ");
        }
    }
}
