using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Padrao canonico de "Contas a receber" (cobranca):
    //  - renomeia 'payment.charge.create' -> 'receivable.charge.create' (dominio espelha a categoria
    //    'contas-a-receber' e o intent 'receivable.*' do AgencyCampaign; 'payment' era ambiguo);
    //  - refina os schemas de create para baterem com o consumidor real (AgencyCampaign FinancialEntryService);
    //  - semeia 'receivable.charge.update' e 'receivable.charge.cancel' (Alterar / Cancelar).
    // Idempotente: renomeia/cria o que falta, no-op onde ja existe. Nao toca em integrations/conectores
    // (dado por tenant). 'contas-a-pagar' (transfer) sera tratado em migration propria.
    [Migration(202606140001)]
    public sealed class Migration_202606140001_ReceivableChargeStandard : Migration
    {
        public override void Up()
        {
            // 1. Renomeia o contrato de emissao, preservando o id (sem recriar)
            Execute.Sql(@"
                UPDATE servicecontract
                   SET identifier = 'receivable.charge.create'
                 WHERE identifier = 'payment.charge.create'
                   AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE identifier = 'receivable.charge.create');
            ");

            // 2. Refina os schemas de receivable.charge.create
            Execute.Sql("""
                UPDATE servicecontract SET
                  name = 'Criar cobranca',
                  description = 'Emite a cobranca de um recebivel (boleto ou PIX) para o pagador.',
                  integrationcategoryid = (SELECT id FROM integrationcategory WHERE identifier = 'contas-a-receber' LIMIT 1),
                  hascallback = true,
                  inputschema = '{
                  "financialEntryId": "number (obrigatório) - id do recebível no Kanvas (referência externa)",
                  "amount": "number (obrigatório) - valor da cobrança",
                  "dueAt": "ISO datetime (obrigatório) - vencimento",
                  "description": "string?",
                  "payerName": "string (obrigatório)",
                  "payerDocument": "string (obrigatório) - CPF/CNPJ do pagador",
                  "method": "pix | boleto (obrigatório)",
                  "fineValue": "number? - multa por atraso (valor)",
                  "interestMonthlyPercent": "number? - juros ao mês (%)",
                  "discountValue": "number? - desconto (valor)",
                  "discountUntil": "ISO date? - desconto válido até",
                  "callbackToken": "string (obrigatório) - ecoar na URL de callback p/ resolver o tenant"
                }',
                  outputschema = '{
                  "chargeId": "string - id da cobrança no provedor",
                  "status": "requested | issued"
                }',
                  callbackschema = '{
                  "eventType": "created | issued | paid | expired | cancelled | failed (obrigatório)",
                  "provider": "string (obrigatório)",
                  "chargeId": "string (obrigatório)",
                  "financialEntryId": "number? - eco do callbackToken",
                  "occurredAt": "ISO datetime?",
                  "paidAt": "ISO datetime? - quando status=paid",
                  "endToEndId": "string? - e2e do PIX",
                  "digitableLine": "string? - linha digitável (boleto)",
                  "barCode": "string? - código de barras (boleto)",
                  "nossoNumero": "string? - nosso número (boleto)",
                  "bankSlipUrl": "string? - PDF do boleto",
                  "pixCopyPaste": "string? - PIX copia-e-cola",
                  "pixQrCodeUrl": "string? - QR code do PIX",
                  "txId": "string? - txid do PIX",
                  "chargeUrl": "string? - link público da cobrança",
                  "metadata": "string?"
                }',
                  updatedat = NOW() AT TIME ZONE 'utc'
                WHERE identifier = 'receivable.charge.create';
                """);

            // 3. Alterar cobranca
            SeedServiceContract(
                identifier: "receivable.charge.update",
                name: "Alterar cobranca",
                description: "Altera uma cobranca ativa (vencimento, multa, juros, desconto ou valor). Alterar valor pode virar cancelar + reemitir no provedor.",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """
                {
                  "chargeId": "string (obrigatório) - cobrança a alterar no provedor",
                  "financialEntryId": "number (obrigatório) - id do recebível no Kanvas",
                  "amount": "number? - novo valor (pode exigir cancelar + reemitir no provedor)",
                  "dueAt": "ISO datetime? - novo vencimento",
                  "fineValue": "number?",
                  "interestMonthlyPercent": "number?",
                  "discountValue": "number?",
                  "discountUntil": "ISO date?",
                  "callbackToken": "string (obrigatório)"
                }
                """,
                outputSchema: """
                {
                  "chargeId": "string - id corrente (novo se houve reemissão)",
                  "status": "updated | reissued",
                  "reissued": "bool"
                }
                """,
                hasCallback: true,
                callbackSchema: """
                {
                  "eventType": "updated | issued | failed (obrigatório)",
                  "provider": "string (obrigatório)",
                  "chargeId": "string (obrigatório) - id corrente (novo se reemitido)",
                  "financialEntryId": "number?",
                  "occurredAt": "ISO datetime?",
                  "digitableLine": "string?",
                  "barCode": "string?",
                  "nossoNumero": "string?",
                  "bankSlipUrl": "string?",
                  "pixCopyPaste": "string?",
                  "pixQrCodeUrl": "string?",
                  "txId": "string?",
                  "metadata": "string?"
                }
                """);

            // 4. Cancelar cobranca
            SeedServiceContract(
                identifier: "receivable.charge.cancel",
                name: "Cancelar cobranca",
                description: "Cancela uma cobranca ativa (baixa o boleto ou cancela o PIX) antes do pagamento.",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """
                {
                  "chargeId": "string (obrigatório) - cobrança a cancelar no provedor",
                  "financialEntryId": "number (obrigatório) - id do recebível no Kanvas",
                  "reason": "string? - motivo do cancelamento",
                  "callbackToken": "string (obrigatório)"
                }
                """,
                outputSchema: """
                {
                  "chargeId": "string",
                  "status": "cancelled | already_paid | not_found"
                }
                """,
                hasCallback: true,
                callbackSchema: """
                {
                  "eventType": "cancelled | failed (obrigatório)",
                  "provider": "string (obrigatório)",
                  "chargeId": "string (obrigatório)",
                  "financialEntryId": "number?",
                  "occurredAt": "ISO datetime?",
                  "metadata": "string?"
                }
                """);
        }

        public override void Down()
        {
            // Convergencia de dados; Down nao reverte para nao reintroduzir o estado anterior.
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
