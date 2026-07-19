using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Refaz o asaas-criar-cobranca para nao duplicar customer no Asaas: busca por cpfCnpj e o
    // "Criar cliente" so roda quando nao existe (runcondition, coluna criada em 202607190003).
    // Estrategia delete+reseed: os INSERTs idempotentes do seed base nao atualizam registros ja
    // existentes, entao os artefatos alterados sao removidos e re-seedados na nova versao. Seguro
    // porque nenhum tenant tem execucoes do asaas (integracao criada hoje, sem conector em prod).
    [Migration(202607190004)]
    public sealed class Migration_202607190004_AsaasClienteCondicional : IntegrationSeedMigration
    {
        public override void Up()
        {
            Execute.Sql("""
                DELETE FROM pipelinestep WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-criar-cobranca');
                DELETE FROM javascriptfunction WHERE name = 'asaas-normalizar-payload';
                DELETE FROM apicall WHERE name = 'Asaas - Criar cobrança';
                """);

            // --- JavaScript functions ---
            SeedJsFunction("asaas-normalizar-payload",
                """
                var doc=(payload.payerDocument==null?'':(''+payload.payerDocument)).replace(/\D/g,'');
                var due=(payload.dueAt==null?'':(''+payload.dueAt)).substring(0,10);
                var cep=(payload.payerCep==null?'':(''+payload.payerCep)).replace(/\D/g,'');
                var juros=Number(payload.interestMonthlyPercent)||0;
                var jurosFrag=juros>0?(',"interest":{"value":'+juros+'}'):'';
                var customerBody={
                  name:(payload.payerName||''),
                  cpfCnpj:doc,
                  postalCode:cep,
                  address:(payload.payerStreet||''),
                  addressNumber:(payload.payerNumber?(''+payload.payerNumber):''),
                  externalReference:(''+(payload.financialEntryId==null?'':payload.financialEntryId))
                };
                result.value={asaasCustomerBody:JSON.stringify(customerBody),asaasCpfCnpj:doc,dueDate:due,jurosFrag:jurosFrag};
                """,
                "Monta o body do POST /customers Asaas e deriva asaasCpfCnpj (para busca de cliente), dueDate (yyyy-MM-dd) e fragmento condicional de juros.");

            SeedJsFunction("asaas-resolver-cliente",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var items=el(variables.data)||[];
                var existing=(items.length>0&&items[0]&&items[0].id)?(''+items[0].id):'';
                result.value={existingCustomerId:existing};
                """,
                "Extrai o id do cliente Asaas encontrado pela busca por cpfCnpj ('' quando não existe; controla a runcondition do Criar cliente).");

            SeedJsFunction("asaas-consolidar-cliente",
                """
                var existing=(''+((variables.existingCustomerId==null)?'':variables.existingCustomerId));
                var created=(''+((variables.id==null)?'':variables.id));
                result.value={customerId:(existing||created)};
                """,
                "Consolida o customerId da cobrança: o cliente existente encontrado pela busca ou o recém-criado (variables.id do POST /customers).");

            // --- ApiCalls ---
            SeedApiCall("Asaas - Buscar cliente", HttpMethodType.Get, "{{base_url}}/customers?cpfCnpj={{asaasCpfCnpj}}&limit=1",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            SeedApiCall("Asaas - Criar cobrança", HttpMethodType.Post, "{{base_url}}/payments",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                """{"customer":"{{customerId}}","billingType":"BOLETO","value":{{amount}},"dueDate":"{{dueDate}}","externalReference":"{{financialEntryId}}"{{jurosFrag}}}""");

            // --- Steps (nova ordem) ---
            SeedStep("asaas-criar-cobranca", 1, "Normalizar payload", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-payload");
            SeedStep("asaas-criar-cobranca", 2, "Buscar cliente", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Buscar cliente");
            SeedStep("asaas-criar-cobranca", 3, "Resolver cliente", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-resolver-cliente");
            SeedStep("asaas-criar-cobranca", 4, "Criar cliente", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Criar cliente");
            SeedStep("asaas-criar-cobranca", 5, "Consolidar cliente", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-consolidar-cliente");
            SeedStep("asaas-criar-cobranca", 6, "Criar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Criar cobrança");
            SeedStep("asaas-criar-cobranca", 7, "Obter linha digitável", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter linha digitável");
            SeedStep("asaas-criar-cobranca", 8, "Obter QR PIX", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter QR PIX");
            SeedStep("asaas-criar-cobranca", 9, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-resposta");
            SeedStep("asaas-criar-cobranca", 10, "Callback issued", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Callback issued", ignoreOnResponse: true);
            SeedStep("asaas-criar-cobranca", 11, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas - Callback falha", ignoreOnResponse: true);

            // runcondition + runonerror: o helper base SeedStep nao seta essas colunas.
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!variables.existingCustomerId', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-criar-cobranca')
                  AND "order" = 4;

                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Asaas - Callback falha')
                  AND pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-criar-cobranca');
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
