using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Sicredi
{
    // Catalogo de Contas a Receber (categoria + contratos cobranca.*) + integracao Sicredi de cobranca.
    // Self-contained e idempotente: convergente (no-op onde ja existe) e completo em tenant novo.
    // base_url default = PRODUCAO (https://api-parceiro.sicredi.com.br). Sandbox usa .../sb no proprio connector.
    // Primeira integracao de cobranca versionada; o catalogo aqui e compartilhado pelas demais (Itau/Sicoob) quando versionadas.
    [Migration(202606210001)]
    public sealed class Migration_202606210001_SeedSicredi : IntegrationSeedMigration
    {
        public override void Up()
        {
            // --- Catalogo Contas a Receber ---
            SeedCategory("contas-a-receber", "Contas a Receber", "Provedores de cobrança e recebíveis (boleto, PIX): registro, baixa e conciliação de liquidados.");

            SeedContract(
                identifier: "cobranca.criar",
                name: "Criar cobrança",
                description: "Registra um boleto/PIX de cobrança no provedor.",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """
                {
                  "financialEntryId": "number (obrigatorio)",
                  "amount": "number (obrigatorio)",
                  "dueAt": "string date (obrigatorio)",
                  "method": "boleto | pix",
                  "payerName": "string",
                  "payerDocument": "string CPF/CNPJ",
                  "payerCep": "string",
                  "payerStreet": "string",
                  "payerNumber": "string",
                  "payerCity": "string",
                  "payerState": "string",
                  "interestMonthlyPercent": "number?",
                  "callbackToken": "string"
                }
                """,
                outputSchema: """
                { "assincrono": "callback 'issued' com chargeId, digitableLine, barCode, nossoNumero, pixCopyPaste, txId" }
                """,
                hasCallback: false,
                callbackSchema: null);

            SeedContract(
                identifier: "cobranca.cancelar",
                name: "Cancelar cobrança",
                description: "Baixa/cancela uma cobrança registrada no provedor.",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """
                { "chargeId": "string (obrigatorio)", "financialEntryId": "number", "callbackToken": "string" }
                """,
                outputSchema: """
                { "assincrono": "callback 'cancelled' com chargeId, financialEntryId" }
                """,
                hasCallback: false,
                callbackSchema: null);

            SeedContract(
                identifier: "cobranca.consultar-liquidados",
                name: "Consultar liquidados",
                description: "Consulta os boletos liquidados (pagos) num dia e retorna a lista normalizada para conciliacao.",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """
                { "dia": "string date (dd/MM/yyyy)", "codigoBeneficiario": "string?" }
                """,
                outputSchema: """
                {"liquidados":"[{ financialEntryId, providerChargeId, paidAt, amountPaid }]"}
                """,
                hasCallback: false,
                callbackSchema: null);

            // --- Integracao Sicredi ---
            SeedIntegration("sicredi", "Sicredi", "Cobrança bancária Sicredi (boleto/PIX) via API Parceiro: registro, baixa e consulta de liquidados.", "contas-a-receber", "https://upload.wikimedia.org/wikipedia/commons/a/ab/Sicredi-logo.png", supportsWebhook: true);

            SeedAttribute("sicredi", "x-api-key", "x-api-key", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Chave de API (x-api-key) do app Sicredi.");
            SeedAttribute("sicredi", "username", "Usuário OAuth", FieldType.Text, required: true, order: 2, group: "Autenticação", description: "Username do grant password.");
            SeedAttribute("sicredi", "password", "Senha OAuth", FieldType.Text, required: true, order: 3, group: "Autenticação", sensitive: true, description: "Password do grant password.");
            SeedAttribute("sicredi", "cooperativa", "Cooperativa", FieldType.Text, required: true, order: 4, group: "Beneficiário", description: "Código da cooperativa (header cooperativa).");
            SeedAttribute("sicredi", "posto", "Posto", FieldType.Text, required: true, order: 5, group: "Beneficiário", description: "Código do posto (header posto).");
            SeedAttribute("sicredi", "codigoBeneficiario", "Código Beneficiário", FieldType.Text, required: true, order: 6, group: "Beneficiário", description: "codigoBeneficiario / codBeneficiario.");
            SeedAttribute("sicredi", "base_url", "Base URL", FieldType.Text, required: true, order: 7, group: "Endpoints", hidden: true, description: "Base da API Sicredi. Produção: https://api-parceiro.sicredi.com.br | Sandbox: .../sb. Oculto.", placeholder: "https://api-parceiro.sicredi.com.br", defaultValue: "https://api-parceiro.sicredi.com.br");
            SeedAttribute("sicredi", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 8, group: "Webhook", hidden: true, description: "Base do AgencyCampaign para o provider-callback (ex: https://agencias.mainstay.com.br).");

            BindContract("sicredi", "cobranca.criar");
            BindContract("sicredi", "cobranca.cancelar");
            BindContract("sicredi", "cobranca.consultar-liquidados");

            // --- JavaScript functions ---
            SeedJsFunction("sicredi-normalizar-payload",
                """
                var doc = (payload.payerDocument == null ? "" : ("" + payload.payerDocument)).replace(/[^0-9A-Za-z]/g, "").toUpperCase();
                var due = (payload.dueAt == null ? "" : ("" + payload.dueAt)).substring(0, 10);
                var method = (payload.method == null ? "" : ("" + payload.method)).toLowerCase();
                var juros = Number(payload.interestMonthlyPercent) || 0;
                var jurosFrag = juros > 0 ? (',"juros":' + juros + ',"tipoJuros":"PERCENTUAL"') : "";
                var cep = (payload.payerCep == null ? "" : ("" + payload.payerCep)).replace(/\D/g, "");
                var street = payload.payerStreet || "";
                var endereco = street + (payload.payerNumber ? (", " + payload.payerNumber) : "");
                result.value = {
                  dataVencimento: due,
                  documento: doc,
                  tipoPessoa: (doc.length > 11 ? "PESSOA_JURIDICA" : "PESSOA_FISICA"),
                  tipoCobranca: (method === "pix" ? "HIBRIDO" : "NORMAL"),
                  jurosFrag: jurosFrag,
                  cep: cep,
                  cidade: (payload.payerCity || ""),
                  uf: (payload.payerState || ""),
                  endereco: endereco
                };
                """,
                "Deriva os campos do boleto Sicredi a partir do payload (valor, PF/PJ, juros condicional, endereço).");

            SeedJsFunction("sicredi-normalizar-liquidados",
                """
                // Normaliza a resposta do liquidados Sicredi para o schema comum.
                // variables.items chega como JsonElement (nao array JS nativo): parseia via ToString()+JSON.parse.
                var raw = variables.items;
                var items = [];
                if (raw != null) {
                  if (typeof raw.length === 'number') { items = raw; }
                  else if (typeof raw.ToString === 'function') { try { items = JSON.parse(raw.ToString()); } catch (e) { items = []; } }
                }
                var out = [];
                for (var i = 0; i < items.length; i++) {
                  var it = items[i];
                  out.push({ financialEntryId: it.seuNumero, providerChargeId: it.nossoNumero, paidAt: it.dataPagamento, amountPaid: it.valorLiquidado });
                }
                result.value = { liquidados: out };
                """,
                "Normaliza a resposta de liquidados do Sicredi para { liquidados: [...] }.");

            // --- ApiCalls ---
            SeedApiCall("Sicredi - Autenticação", HttpMethodType.Post, "{{base_url}}/auth/openapi/token",
                """{"x-api-key":"{{x-api-key}}","context":"COBRANCA","Content-Type":"application/x-www-form-urlencoded"}""",
                "grant_type=password&username={{username}}&password={{password}}&scope=cobranca");

            SeedApiCall("Sicredi - Gerar cobrança", HttpMethodType.Post, "{{base_url}}/cobranca/boleto/v1/boletos",
                """{"x-api-key":"{{x-api-key}}","Authorization":"Bearer {{access_token}}","Content-Type":"application/json","cooperativa":"{{cooperativa}}","posto":"{{posto}}"}""",
                """{"tipoCobranca":"{{tipoCobranca}}","codigoBeneficiario":"{{codigoBeneficiario}}","pagador":{"tipoPessoa":"{{tipoPessoa}}","documento":"{{documento}}","nome":"{{payerName}}","endereco":"{{endereco}}","cidade":"{{cidade}}","uf":"{{uf}}","cep":"{{cep}}"},"especieDocumento":"DUPLICATA_MERCANTIL_INDICACAO","seuNumero":"{{financialEntryId}}","dataVencimento":"{{dataVencimento}}","valor":{{amount}}{{jurosFrag}}}""");

            SeedApiCall("Sicredi - Callback Mainstay", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"sicredi","chargeId":{{nossoNumero | json}},"eventType":"issued","financialEntryId":{{financialEntryId}},"digitableLine":{{linhaDigitavel | json}},"barCode":{{codigoBarras | json}},"nossoNumero":{{nossoNumero | json}},"pixCopyPaste":{{qrCode | json}},"txId":{{txid | json}}}""");

            SeedApiCall("Sicredi - Baixar boleto", HttpMethodType.Patch, "{{base_url}}/cobranca/boleto/v1/boletos/{{chargeId}}/baixa",
                """{"x-api-key":"{{x-api-key}}","Authorization":"Bearer {{access_token}}","Content-Type":"application/json","cooperativa":"{{cooperativa}}","posto":"{{posto}}","codigoBeneficiario":"{{codigoBeneficiario}}"}""",
                "{}");

            SeedApiCall("Sicredi - Callback Cancelamento", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"sicredi","eventType":"cancelled","chargeId":{{chargeId | json}},"financialEntryId":{{financialEntryId}}}""");

            SeedApiCall("Sicredi - Callback Falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"sicredi","eventType":"failed","financialEntryId":{{financialEntryId}},"metadata":{{errorMessage | json}}}""");

            SeedApiCall("Sicredi - Consultar liquidados", HttpMethodType.Get, "{{base_url}}/cobranca/boleto/v1/boletos/liquidados/dia?codigoBeneficiario={{codigoBeneficiario}}&dia={{dia}}",
                """{"x-api-key":"{{x-api-key}}","Authorization":"Bearer {{access_token}}","cooperativa":"{{cooperativa}}","posto":"{{posto}}"}""",
                "");

            // --- Pipelines + Steps ---
            SeedPipeline("sicredi", "sicredi-criar-cobranca", "Criar cobrança", "Registra o boleto/PIX e dispara o callback issued.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.criar");
            SeedStep("sicredi-criar-cobranca", 1, "Normalizar payload", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "sicredi-normalizar-payload");
            SeedStep("sicredi-criar-cobranca", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Sicredi - Autenticação");
            SeedStep("sicredi-criar-cobranca", 3, "Gerar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Sicredi - Gerar cobrança");
            SeedStep("sicredi-criar-cobranca", 4, "Callback issued", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Sicredi - Callback Mainstay", ignoreOnResponse: true);
            SeedStep("sicredi-criar-cobranca", 5, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Sicredi - Callback Falha", ignoreOnResponse: true);

            SeedPipeline("sicredi", "sicredi-cancelar-cobranca", "Cancelar cobrança", "Baixa o boleto e dispara o callback cancelled.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.cancelar");
            SeedStep("sicredi-cancelar-cobranca", 1, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Sicredi - Autenticação");
            SeedStep("sicredi-cancelar-cobranca", 2, "Baixar boleto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Sicredi - Baixar boleto");
            SeedStep("sicredi-cancelar-cobranca", 3, "Callback cancelled", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Sicredi - Callback Cancelamento", ignoreOnResponse: true);
            SeedStep("sicredi-cancelar-cobranca", 4, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Sicredi - Callback Falha", ignoreOnResponse: true);

            SeedPipeline("sicredi", "sicredi-consultar-liquidados", "Consultar liquidados", "Consulta os liquidados do dia e normaliza para conciliação.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.consultar-liquidados");
            SeedStep("sicredi-consultar-liquidados", 1, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Sicredi - Autenticação");
            SeedStep("sicredi-consultar-liquidados", 2, "Consultar liquidados", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Sicredi - Consultar liquidados");
            SeedStep("sicredi-consultar-liquidados", 3, "Normalizar", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "sicredi-normalizar-liquidados");

            // runOnError nos callbacks de falha: o helper base SeedStep nao seta a coluna runonerror
            // (que so existe a partir de 202606170014). Esta migration roda depois, entao setamos aqui.
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Sicredi - Callback Falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'sicredi'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
