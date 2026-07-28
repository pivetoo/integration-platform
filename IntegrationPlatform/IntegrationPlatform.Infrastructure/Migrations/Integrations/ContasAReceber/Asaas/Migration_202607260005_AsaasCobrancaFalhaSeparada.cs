using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Separa "nao criou a cobranca no provedor" de "criou mas nao conseguimos avisar o Mainstay", e
    // cancelamento que falha de emissao que falha. Antes os tres casos mandavam o MESMO callback
    // (eventType 'failed'), o que marcava a cobranca como Failed, liberava o botao "Reprocessar" na tela e
    // gerava um SEGUNDO boleto vivo para o mesmo recebivel.
    //
    // Agora:
    // - novo step captura chargeIdCriado logo apos o POST /payments, entao o caminho de erro sabe se o
    //   boleto existe no provedor mesmo quando a falha foi na linha digitavel, no QR ou no proprio callback;
    // - com boleto criado, o erro dispara 'issued' (reentrega da notificacao) em vez de 'failed';
    // - sem boleto criado, segue disparando 'failed';
    // - o pipeline de cancelamento passa a ter callback proprio (eventType 'cancel_failed'), que no
    //   AgencyCampaign mantem a cobranca como esta e alerta em vez de rebaixar para Failed.
    //
    // Estrategia delete+reseed dos artefatos alterados, igual ao 202607190004.
    [Migration(202607260005)]
    public sealed class Migration_202607260005_AsaasCobrancaFalhaSeparada : IntegrationSeedMigration
    {
        public override void Up()
        {
            Execute.Sql("""
                DELETE FROM pipelinestep WHERE pipelineid IN (SELECT id FROM pipeline WHERE identifier IN ('asaas-criar-cobranca', 'asaas-cancelar-cobranca'));
                """);

            // --- JavaScript functions ---
            SeedJsFunction("asaas-capturar-cobranca",
                """
                result.value={chargeIdCriado:(variables.id==null?'':(''+variables.id))};
                """,
                "Captura o id da cobrança recém-criada logo após o POST /payments. É o que permite ao caminho de erro saber se o boleto existe no provedor (falha depois desse ponto NÃO pode virar 'failed').");

            // --- ApiCalls ---
            SeedApiCall("Asaas - Callback emitida (reentrega)", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"issued","financialEntryId":{{financialEntryId}},"chargeId":{{chargeIdCriado | json}},"metadata":{{errorMessage | json}}}""");

            SeedApiCall("Asaas - Callback cancelamento falhou", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"cancel_failed","financialEntryId":{{financialEntryId}},"chargeId":{{chargeId | json}},"metadata":{{errorMessage | json}}}""");

            // --- Steps: criar cobranca ---
            SeedStep("asaas-criar-cobranca", 1, "Normalizar payload", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-payload");
            SeedStep("asaas-criar-cobranca", 2, "Buscar cliente", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Buscar cliente");
            SeedStep("asaas-criar-cobranca", 3, "Resolver cliente", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-resolver-cliente");
            SeedStep("asaas-criar-cobranca", 4, "Criar cliente", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Criar cliente");
            SeedStep("asaas-criar-cobranca", 5, "Consolidar cliente", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-consolidar-cliente");
            SeedStep("asaas-criar-cobranca", 6, "Criar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Criar cobrança");
            SeedStep("asaas-criar-cobranca", 7, "Capturar cobrança", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-capturar-cobranca");
            SeedStep("asaas-criar-cobranca", 8, "Obter linha digitável", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter linha digitável");
            SeedStep("asaas-criar-cobranca", 9, "Obter QR PIX", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter QR PIX");
            SeedStep("asaas-criar-cobranca", 10, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-resposta");
            SeedStep("asaas-criar-cobranca", 11, "Callback issued", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Callback issued", ignoreOnResponse: true);
            SeedStep("asaas-criar-cobranca", 12, "Callback emitida (reentrega)", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas - Callback emitida (reentrega)", ignoreOnResponse: true);
            SeedStep("asaas-criar-cobranca", 13, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas - Callback falha", ignoreOnResponse: true);

            // --- Steps: cancelar cobranca ---
            SeedStep("asaas-cancelar-cobranca", 1, "Cancelar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Cancelar cobrança");
            SeedStep("asaas-cancelar-cobranca", 2, "Callback cancelled", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Callback cancelled", ignoreOnResponse: true);
            SeedStep("asaas-cancelar-cobranca", 3, "Callback cancelamento falhou", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas - Callback cancelamento falhou", ignoreOnResponse: true);

            // runcondition + runonerror: o helper base SeedStep nao seta essas colunas.
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!variables.existingCustomerId', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-criar-cobranca')
                  AND "order" = 4;

                UPDATE pipelinestep SET runonerror = true, runcondition = 'variables.chargeIdCriado', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-criar-cobranca')
                  AND "order" = 12;

                UPDATE pipelinestep SET runonerror = true, runcondition = '!variables.chargeIdCriado', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-criar-cobranca')
                  AND "order" = 13;

                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-cancelar-cobranca')
                  AND "order" = 3;
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
