using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAPagar.Asaas
{
    // Refaz o asaas-pagamento-pix para parar de afirmar que o repasse foi pago logo apos o POST /transfers
    // ser aceito. O Asaas responde a criacao da transferencia com status ainda em transito (PENDING /
    // BANK_PROCESSING) e a liquidacao pode falhar depois — o callback 'paid' otimista fazia o AgencyCampaign
    // marcar o repasse como Pago com dinheiro que podia nunca ter saido.
    //
    // Agora:
    // - o normalizador expoe 'pagamentoLiquidado' (vocabulario de status concentrado no JS, versionado);
    // - 'Callback pago' so roda quando liquidado (runcondition);
    // - 'Callback aceito' (eventType 'accepted') roda no caso contrario e mantem o repasse Agendado;
    // - o body do /transfers passa a levar externalReference = idempotencyKey, dando correlacao no provedor
    //   (sem ela, uma reexecucao do pipeline nao tinha como ser identificada do lado do Asaas).
    //
    // Estrategia delete+reseed dos artefatos alterados, igual ao 202607190004: os INSERTs idempotentes do
    // seed base nao atualizam registros existentes.
    [Migration(202607260001)]
    public sealed class Migration_202607260001_AsaasPagamentoConfirmacaoReal : IntegrationSeedMigration
    {
        public override void Up()
        {
            Execute.Sql("""
                DELETE FROM pipelinestep WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-pagamento-pix');
                DELETE FROM javascriptfunction WHERE name IN ('asaas-pagamento-normalizar-pix', 'asaas-pagamento-resposta-pix');
                DELETE FROM apicall WHERE name = 'Asaas Pagamento - Transferir PIX';
                """);

            // --- JavaScript functions ---
            SeedJsFunction("asaas-pagamento-normalizar-pix",
                """
                function kt(t){t=(''+(t==null?'':t)).toUpperCase();if(t==='RANDOM'){return 'EVP';}return t;}
                function clean(s){return (''+(s==null?'':s)).replace(/^\s+|\s+$/g,'');}
                var valor=Number(payload.netAmount)||0;
                var body={value:valor,pixAddressKey:clean(payload.pixKey),pixAddressKeyType:kt(payload.pixKeyType),operationType:'PIX',description:clean(payload.description||'Repasse Mainstay').substring(0,100)};
                var ref=clean(payload.idempotencyKey);
                if(ref){body.externalReference=ref;}
                result.value={transferBody:JSON.stringify(body)};
                """,
                "Monta o body do POST /transfers Asaas: value=netAmount, pixAddressKey/pixAddressKeyType (Cpf|Cnpj|Email|Phone->maiusculo, Random->EVP), operationType PIX e externalReference=idempotencyKey (correlação no provedor).");

            SeedJsFunction("asaas-pagamento-resposta-pix",
                """
                function s(v){return v==null?'':(''+v);}
                var st=s(variables.status).toUpperCase();
                var liquidado=(st==='DONE'||st==='CONFIRMED'||st==='RECEIVED');
                result.value={providerTransactionId:s(variables.id),providerPaymentId:s(variables.id),endToEndId:s(variables.endToEndIdentifier),statusAsaas:st,pagamentoLiquidado:liquidado};
                """,
                "Extrai id/status/endToEndIdentifier da transferência Asaas e deriva pagamentoLiquidado: só status terminal de sucesso (DONE/CONFIRMED/RECEIVED) conta como pago; PENDING/BANK_PROCESSING seguem em trânsito.");

            // --- ApiCalls ---
            SeedApiCall("Asaas Pagamento - Transferir PIX", HttpMethodType.Post, "{{base_url}}/transfers",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                "{{transferBody}}");

            SeedApiCall("Asaas Pagamento - Callback aceito", HttpMethodType.Post, "{{callbackBaseUrl}}/api/creatorpayments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"accepted","idempotencyKey":{{idempotencyKey | json}},"providerTransactionId":{{providerTransactionId | json}},"metadata":{{statusAsaas | json}}}""");

            // --- Steps (nova ordem) ---
            SeedStep("asaas-pagamento-pix", 1, "Normalizar pagamento", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-normalizar-pix");
            SeedStep("asaas-pagamento-pix", 2, "Transferir PIX", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Transferir PIX");
            SeedStep("asaas-pagamento-pix", 3, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-resposta-pix");
            SeedStep("asaas-pagamento-pix", 4, "Callback pago", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Callback pago", ignoreOnResponse: true);
            SeedStep("asaas-pagamento-pix", 5, "Callback aceito", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Callback aceito", ignoreOnResponse: true);
            SeedStep("asaas-pagamento-pix", 6, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas Pagamento - Callback falha", ignoreOnResponse: true);

            // runcondition + runonerror: o helper base SeedStep nao seta essas colunas.
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = 'variables.pagamentoLiquidado', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-pagamento-pix')
                  AND "order" = 4;

                UPDATE pipelinestep SET runcondition = '!variables.pagamentoLiquidado', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-pagamento-pix')
                  AND "order" = 5;

                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Asaas Pagamento - Callback falha')
                  AND pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-pagamento-pix');
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
