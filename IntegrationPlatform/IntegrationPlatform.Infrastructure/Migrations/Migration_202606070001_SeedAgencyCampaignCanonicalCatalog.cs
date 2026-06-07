using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Converge categorias e service contracts para o conjunto canonico que o AgencyCampaign assume
    // (IntegrationIntents + IntegrationCategoryIdentifier). Idempotente: cria o que falta, no-op onde existe.
    // Corrige tres lacunas das migrations historicas num tenant novo:
    //  - 'banking' era semeado (180001) e apagado (200006);
    //  - 'contas-a-receber'/'contas-a-pagar' nunca eram criadas, entao os contratos de pagamento nao entravam;
    //  - 'whatsapp' nunca era criado, deixando 'messaging.send'/'messaging' em vez de 'whatsapp.send'/'whatsapp'.
    [Migration(202606070001)]
    public sealed class Migration_202606070001_SeedAgencyCampaignCanonicalCatalog : Migration
    {
        public override void Up()
        {
            SeedCategory("email", "E-mail", "Provedores de envio de e-mail transacional.");
            SeedCategory("whatsapp", "WhatsApp", "Provedores de mensageria WhatsApp.");
            SeedCategory("digital-signature", "Assinatura digital", "Provedores de coleta de assinaturas digitais.");
            SeedCategory("contas-a-receber", "Contas a receber", "Provedores de cobrança (boleto, PIX, cartão).");
            SeedCategory("contas-a-pagar", "Contas a pagar", "Provedores de repasse e pagamento (PIX, TED).");
            SeedCategory("payment", "Pagamento", "Provedores de pagamento e cobrança.");
            SeedCategory("banking", "Conta bancária", "Provedores de sincronização de saldo e extrato de contas bancárias.");

            // Consolida 'messaging' -> 'whatsapp' (o que a 200006 nao conseguiu por falta da categoria)
            Execute.Sql(@"
                UPDATE servicecontract
                SET identifier = 'whatsapp.send',
                    name = 'Envio de mensagem WhatsApp',
                    description = 'Envia mensagem via WhatsApp.',
                    integrationcategoryid = (SELECT id FROM integrationcategory WHERE identifier = 'whatsapp')
                WHERE identifier = 'messaging.send'
                  AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE identifier = 'whatsapp.send');
            ");

            SeedServiceContract(
                identifier: "email.send",
                name: "Envio de e-mail",
                description: "Envia um e-mail transacional para um ou mais destinatários.",
                categoryIdentifier: "email",
                inputSchema: """
                {
                  "to": "string[] (obrigatório)",
                  "cc": "string[]?",
                  "bcc": "string[]?",
                  "subject": "string (obrigatório)",
                  "body": "string (obrigatório)",
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
                identifier: "whatsapp.send",
                name: "Envio de mensagem WhatsApp",
                description: "Envia mensagem via WhatsApp.",
                categoryIdentifier: "whatsapp",
                inputSchema: """
                {
                  "to": "string (E.164, obrigatório)",
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
                  "document": "{filename, contentType, contentBase64 | url} (obrigatório)",
                  "signers": "[{name, email, phone?, role, signatureFields?}] (obrigatório)",
                  "title": "string (obrigatório)",
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
                identifier: "payment.charge.create",
                name: "Criar cobrança",
                description: "Emite cobrança para cliente (boleto, PIX ou cartão).",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """
                {
                  "externalReference": "string (obrigatório)",
                  "amount": "number (centavos, obrigatório)",
                  "dueDate": "ISO date (obrigatório)",
                  "method": "pix | boleto | card (obrigatório)",
                  "payer": "{name, document, email?, phone?} (obrigatório)",
                  "description": "string?"
                }
                """,
                outputSchema: """
                {
                  "chargeId": "string",
                  "status": "pending | paid | canceled",
                  "pix": "{qrCode, copyPaste, expiresAt}?",
                  "boleto": "{barcode, pdfUrl, dueDate}?"
                }
                """,
                hasCallback: true,
                callbackSchema: """
                {
                  "chargeId": "string",
                  "status": "pending | paid | overdue | canceled | refunded",
                  "paidAt": "ISO date?",
                  "amountPaid": "number?"
                }
                """);

            SeedServiceContract(
                identifier: "payment.transfer.create",
                name: "Criar repasse",
                description: "Repassa valor (PIX ou TED) para creator ou fornecedor.",
                categoryIdentifier: "contas-a-pagar",
                inputSchema: """
                {
                  "externalReference": "string (obrigatório)",
                  "amount": "number (centavos, obrigatório)",
                  "method": "pix | ted (obrigatório)",
                  "recipient": "{name, document, pixKey? | {bank, branch, account, accountType}} (obrigatório)",
                  "description": "string?"
                }
                """,
                outputSchema: """
                {
                  "transferId": "string",
                  "status": "processing | completed | failed"
                }
                """,
                hasCallback: true,
                callbackSchema: """
                {
                  "transferId": "string",
                  "status": "completed | failed",
                  "completedAt": "ISO date?",
                  "error": "{code, message}?"
                }
                """);

            // Remove 'messaging' orfa (so se nenhuma integration nem servicecontract a referenciar)
            Execute.Sql(@"
                DELETE FROM integrationcategory
                WHERE identifier = 'messaging'
                  AND NOT EXISTS (SELECT 1 FROM integration WHERE integrationcategoryid = integrationcategory.id)
                  AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE integrationcategoryid = integrationcategory.id);
            ");
        }

        public override void Down()
        {
            // Convergencia de dados; Down nao reverte para nao reintroduzir o estado inconsistente anterior.
        }

        private void SeedCategory(string identifier, string name, string description)
        {
            string escapedName = name.Replace("'", "''");
            string escapedDescription = description.Replace("'", "''");
            Execute.Sql($@"
                INSERT INTO integrationcategory (identifier, name, description, isactive, issystem, createdat, updatedat)
                SELECT '{identifier}', '{escapedName}', '{escapedDescription}', true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = '{identifier}');
            ");
        }

        private void SeedServiceContract(string identifier, string name, string description, string categoryIdentifier, string inputSchema, string outputSchema, bool hasCallback, string? callbackSchema)
        {
            string escapedName = name.Replace("'", "''");
            string escapedDescription = description.Replace("'", "''");
            string escapedInput = inputSchema.Replace("'", "''");
            string escapedOutput = outputSchema.Replace("'", "''");
            string callbackSql = callbackSchema is null ? "NULL" : $"'{callbackSchema.Replace("'", "''")}'";

            Execute.Sql($@"
                INSERT INTO servicecontract (identifier, name, description, integrationcategoryid, inputschema, outputschema, hascallback, callbackschema, isactive, issystem, createdat, updatedat)
                SELECT '{identifier}', '{escapedName}', '{escapedDescription}', (SELECT id FROM integrationcategory WHERE identifier = '{categoryIdentifier}' LIMIT 1), '{escapedInput}', '{escapedOutput}', {hasCallback.ToString().ToLower()}, {callbackSql}, true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM servicecontract WHERE identifier = '{identifier}');
            ");
        }
    }
}
