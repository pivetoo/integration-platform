-- =====================================================================
-- Seed: Integração Asaas no IntegrationPlatform
--
-- Cria categoria "Pagamento" + Integration "Asaas" + atributos +
-- 2 JavaScriptFunctions (prepara body PIX + normaliza webhook) +
-- 2 ApiCalls (POST /v3/transfers + POST callback ao AgencyCampaign) +
-- 2 Pipelines (asaas-send-pix outbound + asaas-webhook inbound)
--
-- IDEMPOTENTE: aborta se ja existir Integration com identifier='asaas'
--
-- Convencao do payload enviado pelo AgencyCampaign no enqueue:
--   {
--     "creatorPaymentId": 123,
--     "grossAmount": 1000.0, "discounts": 0, "netAmount": 1000.0,
--     "description": "...",
--     "method": "Pix",
--     "pixKey": "...", "pixKeyType": "Cpf|Cnpj|Email|Phone|Random",
--     "creatorName": "...", "creatorDocument": "...",
--     "scheduledFor": "2026-..."
--   }
-- =====================================================================

DO $$
DECLARE
    v_category_id      BIGINT;
    v_integration_id   BIGINT;
    v_jsfunc_prepare   BIGINT;
    v_jsfunc_normalize BIGINT;
    v_apicall_transfer BIGINT;
    v_apicall_callback BIGINT;
    v_pipeline_send    BIGINT;
    v_pipeline_webhook BIGINT;
BEGIN
    IF EXISTS (SELECT 1 FROM integration WHERE identifier = 'asaas') THEN
        RAISE NOTICE 'Asaas integration ja existe. Abortando seed.';
        RETURN;
    END IF;

    -- 1) Categoria "Pagamento"
    SELECT id INTO v_category_id
    FROM integrationcategory
    WHERE LOWER(name) = LOWER('Pagamento')
    LIMIT 1;

    IF v_category_id IS NULL THEN
        INSERT INTO integrationcategory (name, description, isactive, createdat)
        VALUES ('Pagamento', 'Gateways para repasses, transferencias e PIX para creators e fornecedores.', true, now())
        RETURNING id INTO v_category_id;
        RAISE NOTICE 'Categoria Pagamento criada id=%', v_category_id;
    ELSE
        RAISE NOTICE 'Categoria Pagamento existente reutilizada id=%', v_category_id;
    END IF;

    -- 2) Integration "Asaas"
    INSERT INTO integration (identifier, name, description, integrationcategoryid, isactive, createdat)
    VALUES (
        'asaas',
        'Asaas',
        'Repasses via PIX ou TED para creators usando o gateway Asaas (https://asaas.com).',
        v_category_id,
        true,
        now()
    )
    RETURNING id INTO v_integration_id;
    RAISE NOTICE 'Integration asaas criada id=%', v_integration_id;

    -- 3) Atributos de configuracao do connector
    -- type 1 = Text
    INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
    VALUES
      (v_integration_id, 'api_token', 'API Token',
       'Token de acesso da conta Asaas (Configuracoes > Integracoes > API).',
       '$aact_...', 1, NULL, true, 1, 'Autenticacao', true, now()),
      (v_integration_id, 'base_url', 'URL base da API',
       'URL base. Use sandbox para testes (https://api-sandbox.asaas.com).',
       'https://api.asaas.com', 1, 'https://api.asaas.com', true, 2, 'Endpoints', false, now()),
      (v_integration_id, 'target_callback_url', 'URL de callback do AgencyCampaign',
       'Endpoint completo (incluindo path) que recebera os eventos normalizados do Asaas.',
       'https://kanvas.mainstay.com.br/api/creatorpayments/ProviderCallback',
       1, NULL, true, 3, 'Webhook', false, now()),
      (v_integration_id, 'target_secret', 'Secret do callback',
       'Valor enviado no header x-webhook-secret. Deve coincidir com Webhooks:ProviderCallbackSecret do AgencyCampaign.',
       NULL, 1, NULL, true, 4, 'Webhook', true, now());

    -- 4) JavaScriptFunction: prepara body para POST /v3/transfers
    INSERT INTO javascriptfunction (name, description, code, createdat)
    VALUES (
        'asaas-prepare-transfer-body',
        'Recebe payload de creatorPayment do AgencyCampaign e produz variaveis prontas para o ApiCall do Asaas.',
$JS$
// Entrada: payload (creatorPaymentId, netAmount, pixKey, pixKeyType, description, scheduledFor)
// Saida em result.value: variaveis do step usadas pelo body do ApiCall

var p = payload || {};

// Mapeia pixKeyType da nossa enum (Cpf/Cnpj/Email/Phone/Random) para o formato do Asaas
var typeMap = {
    'Cpf': 'CPF',
    'Cnpj': 'CNPJ',
    'Email': 'EMAIL',
    'Phone': 'PHONE',
    'Random': 'EVP'
};
var asaasType = typeMap[p.pixKeyType] || 'EVP';

// Limpa pontuacao em chaves CPF/CNPJ/PHONE
var cleanKey = p.pixKey || '';
if (asaasType === 'CPF' || asaasType === 'CNPJ' || asaasType === 'PHONE') {
    cleanKey = cleanKey.replace(/\D/g, '');
}

result.value = {
    transfer_value: p.netAmount,
    transfer_pixkey: cleanKey,
    transfer_pixkey_type: asaasType,
    transfer_description: (p.description || ('Repasse para ' + (p.creatorName || 'creator'))).substring(0, 500),
    transfer_external_ref: 'cp-' + (p.creatorPaymentId || ''),
    transfer_schedule_date: p.scheduledFor ? p.scheduledFor.substring(0, 10) : ''
};
$JS$,
        now()
    )
    RETURNING id INTO v_jsfunc_prepare;

    -- 5) ApiCall: POST /v3/transfers
    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat)
    VALUES (
        'Asaas - Criar transferencia PIX',
        'POST /v3/transfers - cria uma transferencia PIX para a chave informada.',
        2, -- Post
        '{{ base_url }}/v3/transfers',
$HEADERS$
{
    "access_token": "{{ api_token }}",
    "Content-Type": "application/json"
}
$HEADERS$,
$BODY$
{
    "value": {{ transfer_value }},
    "pixAddressKey": "{{ transfer_pixkey }}",
    "pixAddressKeyType": "{{ transfer_pixkey_type }}",
    "description": "{{ transfer_description }}",
    "externalReference": "{{ transfer_external_ref }}",
    "scheduleDate": "{{ transfer_schedule_date }}"
}
$BODY$,
        now()
    )
    RETURNING id INTO v_apicall_transfer;

    -- 6) JavaScriptFunction: normaliza webhook do Asaas
    INSERT INTO javascriptfunction (name, description, code, createdat)
    VALUES (
        'asaas-webhook-to-generic',
        'Normaliza webhook nativo do Asaas (TRANSFER_DONE/FAILED/etc) para o formato CreatorPaymentProviderCallbackRequest do AgencyCampaign.',
$JS$
// Entrada: payload (event, transfer: {id, status, value, ...})
// Saida em result.value: variaveis prontas para o body do ApiCall de callback

var p = payload || {};
var transfer = p.transfer || {};
var event = p.event || '';

// Mapeia evento do Asaas para o catalogo aceito pelo AgencyCampaign:
//   paid, failed, cancelled
var normalizedEvent = 'unknown';
if (event === 'TRANSFER_DONE') {
    normalizedEvent = 'paid';
} else if (event === 'TRANSFER_FAILED' || event === 'TRANSFER_BLOCKED') {
    normalizedEvent = 'failed';
} else if (event === 'TRANSFER_CANCELLED') {
    normalizedEvent = 'cancelled';
}

result.value = {
    cb_provider: 'Asaas',
    cb_provider_tx_id: transfer.id || '',
    cb_event_type: normalizedEvent,
    cb_occurred_at: p.dateCreated || transfer.effectiveDate || transfer.dateCreated || new Date().toISOString(),
    cb_failure_reason: transfer.failReason || '',
    cb_metadata: JSON.stringify({
        rawEvent: event,
        rawStatus: transfer.status,
        externalReference: transfer.externalReference,
        operationType: transfer.operationType
    })
};
$JS$,
        now()
    )
    RETURNING id INTO v_jsfunc_normalize;

    -- 7) ApiCall: POST callback ao AgencyCampaign
    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat)
    VALUES (
        'Asaas - Encaminhar callback ao AgencyCampaign',
        'POST {{ target_callback_url }} - encaminha o evento normalizado para o endpoint generico ProviderCallback de pagamentos.',
        2, -- Post
        '{{ target_callback_url }}',
$HEADERS$
{
    "Content-Type": "application/json",
    "x-webhook-secret": "{{ target_secret }}"
}
$HEADERS$,
$BODY$
{
    "provider": "{{ cb_provider }}",
    "providerTransactionId": "{{ cb_provider_tx_id }}",
    "eventType": "{{ cb_event_type }}",
    "occurredAt": "{{ cb_occurred_at }}",
    "failureReason": "{{ cb_failure_reason }}",
    "metadata": {{ cb_metadata }}
}
$BODY$,
        now()
    )
    RETURNING id INTO v_apicall_callback;

    -- 8) Pipeline outbound: asaas-send-pix
    INSERT INTO pipeline (integrationid, identifier, name, description, isactive, createdat)
    VALUES (
        v_integration_id,
        'asaas-send-pix',
        'Enviar transferencia PIX',
        'Pipeline acionada via enqueue do AgencyCampaign quando um lote de pagamentos e agendado.',
        true,
        now()
    )
    RETURNING id INTO v_pipeline_send;

    -- 9) Pipeline inbound: asaas-webhook (acionada pelo WebhookEntryController generico)
    INSERT INTO pipeline (integrationid, identifier, name, description, isactive, createdat)
    VALUES (
        v_integration_id,
        'asaas-webhook',
        'Receber webhook Asaas',
        'Pipeline acionada automaticamente quando o IntegrationPlatform recebe POST em /api/webhooks/{token}. Normaliza payload e encaminha pro AgencyCampaign.',
        true,
        now()
    )
    RETURNING id INTO v_pipeline_webhook;

    -- 10) Steps do send-pix (JS prepara + HTTP envia)
    -- type: 1=HttpRequest, 2=JavaScriptFunction
    -- erroraction: 1=Stop, 2=Continue
    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES
      (v_pipeline_send, 1, 'Preparar body do Asaas',          2, NULL, v_jsfunc_prepare,   NULL, 1, true, false, now()),
      (v_pipeline_send, 2, 'POST /v3/transfers',              1, v_apicall_transfer, NULL, NULL, 1, true, false, now());

    -- 11) Steps do webhook (JS normaliza + HTTP encaminha)
    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES
      (v_pipeline_webhook, 1, 'Normalizar payload Asaas',      2, NULL, v_jsfunc_normalize, NULL, 1, true, false, now()),
      (v_pipeline_webhook, 2, 'POST callback ao AgencyCampaign', 1, v_apicall_callback,   NULL, NULL, 1, true, false, now());

    RAISE NOTICE 'Seed Asaas concluida. integration=%, pipelines=[%, %], jsfunc=[%, %], apicalls=[%, %]',
        v_integration_id, v_pipeline_send, v_pipeline_webhook,
        v_jsfunc_prepare, v_jsfunc_normalize,
        v_apicall_transfer, v_apicall_callback;
END $$;

-- =====================================================================
-- DOWN (reversao manual)
-- =====================================================================
-- DO $$
-- DECLARE v_integration_id BIGINT;
-- BEGIN
--     SELECT id INTO v_integration_id FROM integration WHERE identifier = 'asaas';
--     IF v_integration_id IS NULL THEN RETURN; END IF;
--
--     DELETE FROM pipelinestepvaluemapping WHERE pipelinestepid IN (
--         SELECT id FROM pipelinestep WHERE pipelineid IN (SELECT id FROM pipeline WHERE integrationid = v_integration_id)
--     );
--     DELETE FROM pipelinestep WHERE pipelineid IN (SELECT id FROM pipeline WHERE integrationid = v_integration_id);
--     DELETE FROM pipeline WHERE integrationid = v_integration_id;
--     DELETE FROM apicall WHERE name LIKE 'Asaas - %';
--     DELETE FROM javascriptfunction WHERE name IN ('asaas-prepare-transfer-body', 'asaas-webhook-to-generic');
--     DELETE FROM connectorattributevalue WHERE integrationattributeid IN (SELECT id FROM integrationattribute WHERE integrationid = v_integration_id);
--     DELETE FROM integrationattribute WHERE integrationid = v_integration_id;
--     DELETE FROM connector WHERE integrationid = v_integration_id;
--     DELETE FROM integration WHERE id = v_integration_id;
-- END $$;
