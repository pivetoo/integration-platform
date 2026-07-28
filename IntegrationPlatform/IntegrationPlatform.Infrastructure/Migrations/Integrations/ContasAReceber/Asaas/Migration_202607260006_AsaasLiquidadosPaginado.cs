using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // A consulta de liquidados pedia limit=100 sem offset e ignorava o hasMore da resposta: a partir do 101o
    // boleto liquidado no mesmo dia, o restante era descartado em silencio — e como a confirmacao de
    // pagamento do Asaas depende so dessa consulta, esses recebiveis ficavam abertos indevidamente.
    //
    // O motor e linear (nao ha laco), entao a paginacao e feita por pares consultar+acumular repetidos, cada
    // par condicionado ao hasMore da pagina anterior. Sao 5 paginas = ate 500 liquidados por dia. Se ainda
    // houver mais, a saida marca truncado=true e o AgencyCampaign registra o corte no log em vez de fingir
    // que processou tudo.
    //
    // O acumulador trafega como STRING JSON entre os steps: variavel de step volta como objeto .NET, entao
    // guardar array puro dependeria de coercao; string faz round-trip limpo.
    [Migration(202607260006)]
    public sealed class Migration_202607260006_AsaasLiquidadosPaginado : IntegrationSeedMigration
    {
        public override void Up()
        {
            Execute.Sql("""
                DELETE FROM pipelinestep WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-consultar-liquidados');
                DELETE FROM javascriptfunction WHERE name IN ('asaas-preparar-consulta', 'asaas-normalizar-liquidados');
                DELETE FROM apicall WHERE name = 'Asaas - Consultar liquidados';
                """);

            // --- JavaScript functions ---
            SeedJsFunction("asaas-preparar-consulta",
                """
                var p=(''+(payload.dia||'')).split('/');
                result.value={diaISO:(p.length===3?(p[2]+'-'+p[1]+'-'+p[0]):(''+(payload.dia||''))),offsetAtual:0,liquidadosAcc:'[]',temMais:false};
                """,
                "Converte o dia (dd/MM/yyyy do AgencyCampaign) para yyyy-MM-dd e inicializa o cursor de paginação (offsetAtual, acumulador, temMais).");

            SeedJsFunction("asaas-acumular-liquidados",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var items=el(variables.data)||[];
                var acc=[];
                try{acc=JSON.parse(''+(variables.liquidadosAcc||'[]'));}catch(e){acc=[];}
                for(var i=0;i<items.length;i++){
                  var it=items[i];
                  var fid=(''+(it.externalReference||''));
                  if(fid){
                    acc.push({financialEntryId:fid,providerChargeId:(''+(it.id||'')),paidAt:(it.clientPaymentDate||it.paymentDate||null),amountPaid:(Number(it.value)||0)});
                  }
                }
                var mais=(variables.hasMore===true||(''+variables.hasMore)==='true');
                result.value={liquidadosAcc:JSON.stringify(acc),temMais:mais,offsetAtual:(Number(variables.offsetAtual)||0)+100};
                """,
                "Acumula a página corrente de cobranças RECEIVED no liquidadosAcc, avança o offset e propaga o hasMore do Asaas em temMais (controla a runcondition do próximo par consultar+acumular).");

            SeedJsFunction("asaas-normalizar-liquidados",
                """
                var acc=[];
                try{acc=JSON.parse(''+(variables.liquidadosAcc||'[]'));}catch(e){acc=[];}
                result.value={liquidados:acc,truncado:(variables.temMais===true||(''+variables.temMais)==='true')};
                """,
                "Entrega o acumulado como { liquidados:[...] } e sinaliza truncado=true quando o Asaas ainda tinha páginas além do teto de 5 (o AgencyCampaign registra o corte no log).");

            // --- ApiCalls ---
            SeedApiCall("Asaas - Consultar liquidados", HttpMethodType.Get, "{{base_url}}/payments?paymentDate[ge]={{diaISO}}&paymentDate[le]={{diaISO}}&status=RECEIVED&limit=100&offset={{offsetAtual}}",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            // --- Steps ---
            SeedStep("asaas-consultar-liquidados", 1, "Preparar consulta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-preparar-consulta");
            SeedStep("asaas-consultar-liquidados", 2, "Consultar liquidados", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Consultar liquidados");
            SeedStep("asaas-consultar-liquidados", 3, "Acumular página", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-acumular-liquidados");
            SeedStep("asaas-consultar-liquidados", 4, "Consultar liquidados (página 2)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Consultar liquidados");
            SeedStep("asaas-consultar-liquidados", 5, "Acumular página 2", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-acumular-liquidados");
            SeedStep("asaas-consultar-liquidados", 6, "Consultar liquidados (página 3)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Consultar liquidados");
            SeedStep("asaas-consultar-liquidados", 7, "Acumular página 3", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-acumular-liquidados");
            SeedStep("asaas-consultar-liquidados", 8, "Consultar liquidados (página 4)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Consultar liquidados");
            SeedStep("asaas-consultar-liquidados", 9, "Acumular página 4", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-acumular-liquidados");
            SeedStep("asaas-consultar-liquidados", 10, "Consultar liquidados (página 5)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Consultar liquidados");
            SeedStep("asaas-consultar-liquidados", 11, "Acumular página 5", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-acumular-liquidados");
            SeedStep("asaas-consultar-liquidados", 12, "Normalizar", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-liquidados");

            // Cada par consultar+acumular a partir da 2a pagina so roda se a pagina anterior indicou hasMore.
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = 'variables.temMais', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-consultar-liquidados')
                  AND "order" BETWEEN 4 AND 11;
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
