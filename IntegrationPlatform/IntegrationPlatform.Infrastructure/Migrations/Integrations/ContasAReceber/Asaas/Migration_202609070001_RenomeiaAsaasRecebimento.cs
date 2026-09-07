using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // A integracao de cobranca nascia como "asaas" enquanto a de pagamento e "asaas-pagamento": no painel
    // do Asaas os dois webhooks apareciam como "Mainstay" e "Mainstay Pagamentos", com URLs terminadas em
    // /asaas e /asaas-pagamento, o que confundia o operador. Renomeia para "asaas-recebimento" /
    // "Mainstay Recebimentos" e alinha tudo que deriva do identificador:
    // - pipelines resolvidos por convencao de nome ({identifier}-webhook no receptor e
    //   {identifier}-registrar-webhook no ConnectorService.RegisterWebhook);
    // - URL alvo do webhook e migracao do webhook ja registrado na conta Asaas: o normalizador passa a
    //   reconhecer tambem a URL legada (/asaas) e o passo Atualizar faz o PUT com nome e URL novos,
    //   sem duplicar nem perder o authToken em uso;
    // - "provider" devolvido nos callbacks de cobranca, que o Mainstay compara com ChargeProvider
    //   (= identificador da integracao do conector) na correlacao (provider, chargeId).
    // Funcoes JS, ApiCalls e vinculos de contrato mantem os nomes: sao referenciados por id.
    // UPDATE de catalogo nao tem forma fluente no FluentMigrator, por isso Execute.Sql.
    [Migration(202609070001)]
    public sealed class Migration_202609070001_RenomeiaAsaasRecebimento : Migration
    {
        private const string PrepararRegistroLegado =
            "result.value={webhookTargetUrl:'https://integrations.mainstay.com.br/api/webhooks/'+(variables.tenantId||'')+'/asaas'};";

        private const string PrepararRegistroNovo =
            "var base='https://integrations.mainstay.com.br/api/webhooks/'+(variables.tenantId||'');\n" +
            "result.value={webhookTargetUrl:base+'/asaas-recebimento',legacyWebhookUrl:base+'/asaas'};";

        private const string NormalizarLegado =
            "function el(v){if(v==null){return [];}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return [];}}return v;}\n" +
            "var items=el(variables.data);\n" +
            "var target=''+(variables.webhookTargetUrl||'');\n" +
            "var found=null;\n" +
            "for(var i=0;i<items.length;i++){if((''+(items[i].url||''))===target){found=items[i];break;}}\n" +
            "result.value={existingWebhookId:found?(''+found.id):'',hasExistingWebhook:!!found};";

        private const string NormalizarNovo =
            "function el(v){if(v==null){return [];}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return [];}}return v;}\n" +
            "var items=el(variables.data);\n" +
            "var target=''+(variables.webhookTargetUrl||'');\n" +
            "var legacy=''+(variables.legacyWebhookUrl||'');\n" +
            "var found=null;\n" +
            "for(var i=0;i<items.length;i++){if((''+(items[i].url||''))===target){found=items[i];break;}}\n" +
            "if(!found&&legacy){for(var j=0;j<items.length;j++){if((''+(items[j].url||''))===legacy){found=items[j];break;}}}\n" +
            "result.value={existingWebhookId:found?(''+found.id):'',hasExistingWebhook:!!found};";

        public override void Up()
        {
            Execute.Sql("UPDATE integration SET identifier = 'asaas-recebimento', updatedat = now() WHERE identifier = 'asaas';");
            Execute.Sql("UPDATE pipeline SET identifier = 'asaas-recebimento-webhook', updatedat = now() WHERE identifier = 'asaas-webhook';");
            Execute.Sql("UPDATE pipeline SET identifier = 'asaas-recebimento-registrar-webhook', updatedat = now() WHERE identifier = 'asaas-registrar-webhook';");

            Execute.Sql($"UPDATE javascriptfunction SET code = {Txt(PrepararRegistroNovo)}, description = {Txt("Monta a URL do nosso receptor de webhook (path {tenantId}/asaas-recebimento) e a URL legada ({tenantId}/asaas), usadas para achar/criar/atualizar o webhook na Asaas.")}, updatedat = now() WHERE name = 'asaas-preparar-registro-webhook';");
            Execute.Sql($"UPDATE javascriptfunction SET code = {Txt(NormalizarNovo)}, description = {Txt("Procura, na lista de webhooks da conta Asaas, um com a URL alvo (webhookTargetUrl) ou, na falta, com a URL legada (legacyWebhookUrl) para migra-lo. Define existingWebhookId/hasExistingWebhook para decidir Criar vs Atualizar.")}, updatedat = now() WHERE name = 'asaas-normalizar-webhooks-existentes';");

            Execute.Sql("UPDATE apicall SET bodytemplate = replace(bodytemplate, '\"name\":\"Mainstay\",', '\"name\":\"Mainstay Recebimentos\",'), updatedat = now() WHERE name IN ('Asaas - Criar webhook', 'Asaas - Atualizar webhook');");
            Execute.Sql("UPDATE apicall SET bodytemplate = replace(bodytemplate, '\"provider\":\"asaas\"', '\"provider\":\"asaas-recebimento\"'), updatedat = now() WHERE name LIKE 'Asaas - Callback %';");
        }

        public override void Down()
        {
            Execute.Sql("UPDATE apicall SET bodytemplate = replace(bodytemplate, '\"provider\":\"asaas-recebimento\"', '\"provider\":\"asaas\"'), updatedat = now() WHERE name LIKE 'Asaas - Callback %';");
            Execute.Sql("UPDATE apicall SET bodytemplate = replace(bodytemplate, '\"name\":\"Mainstay Recebimentos\",', '\"name\":\"Mainstay\",'), updatedat = now() WHERE name IN ('Asaas - Criar webhook', 'Asaas - Atualizar webhook');");

            Execute.Sql($"UPDATE javascriptfunction SET code = {Txt(NormalizarLegado)}, description = {Txt("Procura, na lista de webhooks da conta Asaas, um com a mesma URL alvo (webhookTargetUrl). Define existingWebhookId/hasExistingWebhook para decidir Criar vs Atualizar.")}, updatedat = now() WHERE name = 'asaas-normalizar-webhooks-existentes';");
            Execute.Sql($"UPDATE javascriptfunction SET code = {Txt(PrepararRegistroLegado)}, description = {Txt("Monta a URL do nosso receptor de webhook (path {tenantId}/asaas) usada para achar/criar/atualizar o webhook na Asaas.")}, updatedat = now() WHERE name = 'asaas-preparar-registro-webhook';");

            Execute.Sql("UPDATE pipeline SET identifier = 'asaas-registrar-webhook', updatedat = now() WHERE identifier = 'asaas-recebimento-registrar-webhook';");
            Execute.Sql("UPDATE pipeline SET identifier = 'asaas-webhook', updatedat = now() WHERE identifier = 'asaas-recebimento-webhook';");
            Execute.Sql("UPDATE integration SET identifier = 'asaas', updatedat = now() WHERE identifier = 'asaas-recebimento';");
        }

        private static string Txt(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
