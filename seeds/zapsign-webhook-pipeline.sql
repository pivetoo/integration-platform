-- =====================================================================
-- Seed: Pipeline de webhook do ZapSign
--
-- Adiciona ao conector ZapSign existente (criado por zapsign-integration.sql):
--   - 2 atributos novos: target_callback_url, target_secret
--   - 1 JavaScriptFunction "zapsign-payload-to-generic" para normalizar payload
--   - 1 ApiCall "ZapSign - Encaminhar callback ao AgencyCampaign"
--   - 1 Pipeline "zapsign-webhook" com 2 steps (JS + HTTP)
--
-- IDEMPOTENTE: aborta se ja existir Pipeline com identifier='zapsign-webhook'
-- =====================================================================

DO $$
DECLARE
    v_integration_id BIGINT;
    v_apicall_forward BIGINT;
    v_jsfunc_normalize BIGINT;
    v_pipeline_webhook BIGINT;
    v_step_js BIGINT;
    v_step_http BIGINT;
BEGIN
    SELECT id INTO v_integration_id FROM integration WHERE identifier = 'zapsign';
    IF v_integration_id IS NULL THEN
        RAISE EXCEPTION 'Integration zapsign nao encontrada. Rode zapsign-integration.sql primeiro.';
    END IF;

    IF EXISTS (SELECT 1 FROM pipeline WHERE integrationid = v_integration_id AND identifier = 'zapsign-webhook') THEN
        RAISE NOTICE 'Pipeline zapsign-webhook ja existe. Abortando seed.';
        RETURN;
    END IF;

    -- 1) Atributos novos no conector (so insere se nao existir)
    IF NOT EXISTS (SELECT 1 FROM integrationattribute WHERE integrationid = v_integration_id AND field = 'target_callback_url') THEN
        INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
        VALUES (
            v_integration_id, 'target_callback_url', 'URL de callback do AgencyCampaign',
            'Endpoint completo (incluindo path) que recebera os eventos normalizados do ZapSign.',
            'https://kanvas.mainstay.com.br/api/campaigndocuments/ProviderCallback',
            1, NULL, true, 4, 'Webhook', false, now()
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM integrationattribute WHERE integrationid = v_integration_id AND field = 'target_secret') THEN
        INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
        VALUES (
            v_integration_id, 'target_secret', 'Secret do callback',
            'Valor enviado no header x-webhook-secret. Deve coincidir com Webhooks:ProviderCallbackSecret do AgencyCampaign.',
            NULL, 1, NULL, true, 5, 'Webhook', true, now()
        );
    END IF;

    -- 2) JavaScriptFunction de normalizacao
    INSERT INTO javascriptfunction (name, description, code, createdat)
    VALUES (
        'zapsign-payload-to-generic',
        'Normaliza payload nativo do webhook ZapSign para o formato CampaignDocumentProviderCallbackRequest do AgencyCampaign.',
$JS$
// Entrada: payload (JSON nativo do ZapSign)
// Saida em result.value: campos prontos para o body template do step HTTP

var p = payload || {};
var signer = p.signer_who_signed || {};

var providerEvent = p.event_type || 'unknown';
var status = p.status || '';

// Mapeia para o catalogo de event_type aceito pelo AgencyCampaign:
//   created, sent, viewed, signed, doc_signed, completed, rejected, cancelled
var normalizedEvent = providerEvent;
if (providerEvent === 'doc_signed' && status === 'signed') {
    normalizedEvent = 'completed';
} else if (providerEvent === 'doc_signed') {
    normalizedEvent = 'signed';
} else if (providerEvent === 'doc_refused') {
    normalizedEvent = 'rejected';
} else if (providerEvent === 'doc_deleted') {
    normalizedEvent = 'cancelled';
}

result.value = {
    provider: 'ZapSign',
    providerDocumentId: p.token || '',
    eventType: normalizedEvent,
    occurredAt: p.last_update_at || new Date().toISOString(),
    signerEmail: signer.email || '',
    providerSignerId: signer.token || '',
    ipAddress: signer.ip || '',
    userAgent: '',
    signedDocumentUrl: p.signed_file || '',
    metadata: JSON.stringify({
        rawEvent: providerEvent,
        rawStatus: status,
        externalId: p.external_id,
        openId: p.open_id
    })
};
$JS$,
        now()
    )
    RETURNING id INTO v_jsfunc_normalize;

    -- 3) ApiCall que encaminha pro AgencyCampaign
    -- O body usa as variaveis produzidas pelo step JS ({{ provider }}, {{ providerDocumentId }} etc)
    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat)
    VALUES (
        'ZapSign - Encaminhar callback ao AgencyCampaign',
        'POST {{ target_callback_url }} - encaminha o evento normalizado pro endpoint generico ProviderCallback.',
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
    "provider": "{{ provider }}",
    "providerDocumentId": "{{ providerDocumentId }}",
    "eventType": "{{ eventType }}",
    "occurredAt": "{{ occurredAt }}",
    "signerEmail": "{{ signerEmail }}",
    "providerSignerId": "{{ providerSignerId }}",
    "ipAddress": "{{ ipAddress }}",
    "userAgent": "{{ userAgent }}",
    "signedDocumentUrl": "{{ signedDocumentUrl }}",
    "metadata": {{ metadata }}
}
$BODY$,
        now()
    )
    RETURNING id INTO v_apicall_forward;

    -- 4) Pipeline zapsign-webhook
    INSERT INTO pipeline (integrationid, identifier, name, description, isactive, createdat)
    VALUES (
        v_integration_id,
        'zapsign-webhook',
        'Receber webhook ZapSign',
        'Pipeline acionada automaticamente quando o IntegrationPlatform recebe POST em /api/webhooks/{token}. Normaliza payload e encaminha pro AgencyCampaign.',
        true,
        now()
    )
    RETURNING id INTO v_pipeline_webhook;

    -- 5) Step 1: JS de normalizacao
    -- type: 1=HttpRequest, 2=JavaScriptFunction, 3=ExecuteScript
    -- erroraction: 1=Stop, 2=Continue
    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES (
        v_pipeline_webhook,
        1,
        'Normalizar payload ZapSign',
        2, -- JavaScriptFunction
        NULL,
        v_jsfunc_normalize,
        NULL,
        1, -- Stop on error
        true,
        false,
        now()
    )
    RETURNING id INTO v_step_js;

    -- 6) Step 2: HTTP forward
    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES (
        v_pipeline_webhook,
        2,
        'POST callback ao AgencyCampaign',
        1, -- HttpRequest
        v_apicall_forward,
        NULL,
        NULL,
        1, -- Stop on error
        true,
        false,
        now()
    )
    RETURNING id INTO v_step_http;

    RAISE NOTICE 'Pipeline zapsign-webhook criada id=%, steps=[%, %], jsfunc=%, apicall=%',
        v_pipeline_webhook, v_step_js, v_step_http, v_jsfunc_normalize, v_apicall_forward;
END $$;

-- =====================================================================
-- DOWN (reversao manual - descomentar e executar se precisar remover)
-- =====================================================================
-- DO $$
-- DECLARE
--     v_integration_id BIGINT;
--     v_pipeline_id BIGINT;
-- BEGIN
--     SELECT id INTO v_integration_id FROM integration WHERE identifier = 'zapsign';
--     IF v_integration_id IS NULL THEN RETURN; END IF;
--
--     SELECT id INTO v_pipeline_id FROM pipeline WHERE integrationid = v_integration_id AND identifier = 'zapsign-webhook';
--     IF v_pipeline_id IS NULL THEN RETURN; END IF;
--
--     DELETE FROM pipelinestepvaluemapping WHERE pipelinestepid IN (SELECT id FROM pipelinestep WHERE pipelineid = v_pipeline_id);
--     DELETE FROM pipelinestep WHERE pipelineid = v_pipeline_id;
--     DELETE FROM pipeline WHERE id = v_pipeline_id;
--     DELETE FROM apicall WHERE name = 'ZapSign - Encaminhar callback ao AgencyCampaign';
--     DELETE FROM javascriptfunction WHERE name = 'zapsign-payload-to-generic';
--     DELETE FROM integrationattribute WHERE integrationid = v_integration_id AND field IN ('target_callback_url', 'target_secret');
-- END $$;
