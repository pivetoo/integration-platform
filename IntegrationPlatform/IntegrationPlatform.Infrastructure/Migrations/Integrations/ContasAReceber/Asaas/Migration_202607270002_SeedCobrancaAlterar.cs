using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Cria o contrato cobranca.alterar (que NAO existia em lugar nenhum do IntegrationPlatform, apesar de
    // o AgencyCampaign ja consumi-lo) e o pipeline asaas-alterar-cobranca.
    //
    // Sem ele, com o boleto emitido o operador nao tinha NENHUM caminho para corrigir valor ou vencimento:
    // a edicao do lancamento fica travada e o endpoint update-charge falhava com serviceContract.notFound.
    //
    // Desenho: o Asaas atualiza a cobranca em PUT /payments/{id} mantendo o mesmo id, mas exige os campos
    // principais no corpo. Por isso o pipeline le a cobranca atual ANTES (GET) e faz merge com o que veio
    // no payload — assim alterar so o vencimento nao zera o valor. Alterar valor/vencimento regenera a
    // ficha, entao linha digitavel e QR sao buscados de novo e vao no callback 'updated'.
    [Migration(202607270002)]
    public sealed class Migration_202607270002_SeedCobrancaAlterar : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedContract(
                identifier: "cobranca.alterar",
                name: "Alterar cobrança",
                description: "Altera valor, vencimento e encargos de uma cobrança já registrada no provedor. Confirma por callback 'updated' com os artefatos regerados.",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """
                {
                  "chargeId": "string (obrigatorio)",
                  "financialEntryId": "number (obrigatorio)",
                  "amount": "number?",
                  "dueAt": "string date?",
                  "fineValue": "number?",
                  "interestMonthlyPercent": "number?",
                  "discountValue": "number?",
                  "discountUntil": "string date?",
                  "callbackToken": "string"
                }
                """,
                outputSchema: """
                { "assincrono": "callback 'updated' com chargeId, digitableLine, barCode, nossoNumero, pixCopyPaste" }
                """,
                hasCallback: false,
                callbackSchema: null);

            BindContract("asaas", "cobranca.alterar");

            // --- JavaScript functions ---
            SeedJsFunction("asaas-normalizar-alteracao",
                """
                function s(v){return v==null?'':(''+v);}
                var valorAtual=Number(variables.value)||0;
                var dueAtual=s(variables.dueDate).substring(0,10);
                var valor=Number(payload.amount)||0;
                var due=s(payload.dueAt).substring(0,10);
                var body={billingType:'BOLETO',value:(valor>0?valor:valorAtual),dueDate:(due?due:dueAtual)};
                var juros=Number(payload.interestMonthlyPercent)||0;
                if(juros>0){body.interest={value:juros};}
                var multa=Number(payload.fineValue)||0;
                if(multa>0){body.fine={value:multa,type:'FIXED'};}
                var desconto=Number(payload.discountValue)||0;
                if(desconto>0){
                  var limite=0;
                  var ate=s(payload.discountUntil).substring(0,10);
                  if(ate&&body.dueDate){
                    try{
                      var pa=ate.split('-');var pd=(''+body.dueDate).split('-');
                      var ma=Date.UTC(Number(pa[0]),Number(pa[1])-1,Number(pa[2]));
                      var md=Date.UTC(Number(pd[0]),Number(pd[1])-1,Number(pd[2]));
                      limite=Math.round((md-ma)/86400000);
                      if(!(limite>0)){limite=0;}
                    }catch(e){limite=0;}
                  }
                  body.discount={value:desconto,dueDateLimitDays:limite,type:'FIXED'};
                }
                result.value={alteracaoBody:JSON.stringify(body)};
                """,
                "Monta o body do PUT /payments/{id}: faz merge do que veio no payload com o valor e o vencimento ATUAIS da cobrança (lidos no step anterior), para alterar só um campo não zerar o outro.");

            // --- ApiCalls ---
            SeedApiCall("Asaas - Obter cobrança", HttpMethodType.Get, "{{base_url}}/payments/{{chargeId}}",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            SeedApiCall("Asaas - Alterar cobrança", HttpMethodType.Put, "{{base_url}}/payments/{{chargeId}}",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                "{{alteracaoBody}}");

            SeedApiCall("Asaas - Callback updated", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"updated","financialEntryId":{{financialEntryId}},"chargeId":{{chargeId | json}},"digitableLine":{{digitableLine | json}},"barCode":{{barCode | json}},"nossoNumero":{{nossoNumeroAsaas | json}},"pixCopyPaste":{{pixCopyPaste | json}},"chargeUrl":{{chargeUrlAsaas | json}},"bankSlipUrl":{{bankSlipUrlAsaas | json}}}""");

            SeedApiCall("Asaas - Callback alteracao falhou", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"update_failed","financialEntryId":{{financialEntryId}},"chargeId":{{chargeId | json}},"metadata":{{errorMessage | json}}}""");

            // --- Pipeline + Steps ---
            SeedPipeline("asaas", "asaas-alterar-cobranca", "Alterar cobrança", "Lê a cobrança atual, aplica a alteração (PUT /payments), rebusca linha digitável e QR e dispara callback updated.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.alterar");
            SeedStep("asaas-alterar-cobranca", 1, "Obter cobrança atual", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter cobrança");
            SeedStep("asaas-alterar-cobranca", 2, "Normalizar alteração", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-alteracao");
            SeedStep("asaas-alterar-cobranca", 3, "Alterar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Alterar cobrança");
            SeedStep("asaas-alterar-cobranca", 4, "Obter linha digitável", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter linha digitável");
            SeedStep("asaas-alterar-cobranca", 5, "Obter QR PIX", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter QR PIX");
            SeedStep("asaas-alterar-cobranca", 6, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-resposta");
            SeedStep("asaas-alterar-cobranca", 7, "Callback updated", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Callback updated", ignoreOnResponse: true);
            SeedStep("asaas-alterar-cobranca", 8, "Callback alteracao falhou", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas - Callback alteracao falhou", ignoreOnResponse: true);

            // runOnError no callback de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-alterar-cobranca')
                  AND "order" = 8;
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
