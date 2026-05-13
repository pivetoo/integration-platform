-- =====================================================================
-- Seed: Steps de normalização para pipelines de webhook WhatsApp
--
-- Adiciona em cada integração WhatsApp:
--   - 1 atributo novo: callback_secret (IntegrationSecret do AgencyCampaign)
--   - 1 JavaScriptFunction para normalizar o payload do provedor
--   - 1 ApiCall para encaminhar ao AgencyCampaign (/api/whatsapp/webhook)
--   - 2 PipelineSteps (JS + HTTP) no pipeline de webhook já existente
--
-- Provedores cobertos: z-api, evolution-api, whatsapp-cloud, twilio-whatsapp, 360dialog
--
-- IDEMPOTENTE: cada bloco aborta se o pipeline já tiver steps.
-- =====================================================================


-- =====================================================================
-- Z-API
-- =====================================================================
DO $$
DECLARE
    v_integration_id BIGINT;
    v_pipeline_id    BIGINT;
    v_jsfunc_id      BIGINT;
    v_apicall_id     BIGINT;
BEGIN
    SELECT id INTO v_integration_id FROM integration WHERE identifier = 'z-api';
    IF v_integration_id IS NULL THEN
        RAISE EXCEPTION 'Integration z-api nao encontrada.';
    END IF;

    SELECT id INTO v_pipeline_id FROM pipeline WHERE integrationid = v_integration_id AND identifier = 'z-api-webhook';
    IF v_pipeline_id IS NULL THEN
        RAISE EXCEPTION 'Pipeline z-api-webhook nao encontrada.';
    END IF;

    IF EXISTS (SELECT 1 FROM pipelinestep WHERE pipelineid = v_pipeline_id) THEN
        RAISE NOTICE 'Pipeline z-api-webhook ja possui steps. Abortando.';
        RETURN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM integrationattribute WHERE integrationid = v_integration_id AND field = 'callback_secret') THEN
        INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
        VALUES (v_integration_id, 'callback_secret', 'Secret do callback', 'IntegrationSecret do AgencyCampaign. Enviado no header X-Integration-Secret para autenticar o callback.', NULL, 1, NULL, true, 6, 'Webhook', true, now());
    END IF;

    INSERT INTO javascriptfunction (name, description, code, createdat) VALUES (
        'z-api-normalize-message',
        'Normaliza payload de mensagem recebida do Z-API para o formato padrão do inbox WhatsApp.',
$JS$
var from = (payload.phone || '').replace('@s.whatsapp.net', '').replace('@c.us', '');
var message = (payload.text && payload.text.message) || '';
var timestamp = payload.momment || Math.floor(Date.now() / 1000);

if (payload.fromMe || !from || !message) {
    result.value = null;
} else {
    result.value = { from: from, message: message, timestamp: timestamp };
}
$JS$,
        now()
    ) RETURNING id INTO v_jsfunc_id;

    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat) VALUES (
        'Z-API - Encaminhar mensagem ao AgencyCampaign',
        'POST target_callback_url com payload normalizado da mensagem recebida.',
        2,
        '{{ target_callback_url }}',
$HEADERS$
{
    "Content-Type": "application/json",
    "X-Integration-Secret": "{{ callback_secret }}"
}
$HEADERS$,
$BODY$
{
    "from": "{{ from }}",
    "message": "{{ message }}",
    "timestamp": {{ timestamp }}
}
$BODY$,
        now()
    ) RETURNING id INTO v_apicall_id;

    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES
        (v_pipeline_id, 1, 'Normalizar payload Z-API',           2, NULL,         v_jsfunc_id, NULL, 1, true, false, now()),
        (v_pipeline_id, 2, 'POST mensagem ao AgencyCampaign',    1, v_apicall_id, NULL,        NULL, 2, true, false, now());

    RAISE NOTICE 'z-api-webhook: steps criados (jsfunc=%, apicall=%)', v_jsfunc_id, v_apicall_id;
END $$;


-- =====================================================================
-- Evolution API
-- =====================================================================
DO $$
DECLARE
    v_integration_id BIGINT;
    v_pipeline_id    BIGINT;
    v_jsfunc_id      BIGINT;
    v_apicall_id     BIGINT;
BEGIN
    SELECT id INTO v_integration_id FROM integration WHERE identifier = 'evolution-api';
    IF v_integration_id IS NULL THEN
        RAISE EXCEPTION 'Integration evolution-api nao encontrada.';
    END IF;

    SELECT id INTO v_pipeline_id FROM pipeline WHERE integrationid = v_integration_id AND identifier = 'evolution-webhook';
    IF v_pipeline_id IS NULL THEN
        RAISE EXCEPTION 'Pipeline evolution-webhook nao encontrada.';
    END IF;

    IF EXISTS (SELECT 1 FROM pipelinestep WHERE pipelineid = v_pipeline_id) THEN
        RAISE NOTICE 'Pipeline evolution-webhook ja possui steps. Abortando.';
        RETURN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM integrationattribute WHERE integrationid = v_integration_id AND field = 'callback_secret') THEN
        INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
        VALUES (v_integration_id, 'callback_secret', 'Secret do callback', 'IntegrationSecret do AgencyCampaign. Enviado no header X-Integration-Secret para autenticar o callback.', NULL, 1, NULL, true, 5, 'Webhook', true, now());
    END IF;

    INSERT INTO javascriptfunction (name, description, code, createdat) VALUES (
        'evolution-api-normalize-message',
        'Normaliza payload de mensagem recebida da Evolution API para o formato padrão do inbox WhatsApp.',
$JS$
var data = payload.data || {};
var key = data.key || {};

if (key.fromMe) {
    result.value = null;
} else {
    var remoteJid = key.remoteJid || '';
    var from = remoteJid.replace(/@[^@]*$/, '');
    var msg = data.message || {};
    var message = msg.conversation
        || (msg.extendedTextMessage && msg.extendedTextMessage.text)
        || '';
    var timestamp = parseInt(data.messageTimestamp) || Math.floor(Date.now() / 1000);

    if (!from || !message) {
        result.value = null;
    } else {
        result.value = { from: from, message: message, timestamp: timestamp };
    }
}
$JS$,
        now()
    ) RETURNING id INTO v_jsfunc_id;

    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat) VALUES (
        'Evolution API - Encaminhar mensagem ao AgencyCampaign',
        'POST target_callback_url com payload normalizado da mensagem recebida.',
        2,
        '{{ target_callback_url }}',
$HEADERS$
{
    "Content-Type": "application/json",
    "X-Integration-Secret": "{{ callback_secret }}"
}
$HEADERS$,
$BODY$
{
    "from": "{{ from }}",
    "message": "{{ message }}",
    "timestamp": {{ timestamp }}
}
$BODY$,
        now()
    ) RETURNING id INTO v_apicall_id;

    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES
        (v_pipeline_id, 1, 'Normalizar payload Evolution API',   2, NULL,         v_jsfunc_id, NULL, 1, true, false, now()),
        (v_pipeline_id, 2, 'POST mensagem ao AgencyCampaign',    1, v_apicall_id, NULL,        NULL, 2, true, false, now());

    RAISE NOTICE 'evolution-webhook: steps criados (jsfunc=%, apicall=%)', v_jsfunc_id, v_apicall_id;
END $$;


-- =====================================================================
-- WhatsApp Cloud API (Meta)
-- =====================================================================
DO $$
DECLARE
    v_integration_id BIGINT;
    v_pipeline_id    BIGINT;
    v_jsfunc_id      BIGINT;
    v_apicall_id     BIGINT;
BEGIN
    SELECT id INTO v_integration_id FROM integration WHERE identifier = 'whatsapp-cloud';
    IF v_integration_id IS NULL THEN
        RAISE EXCEPTION 'Integration whatsapp-cloud nao encontrada.';
    END IF;

    SELECT id INTO v_pipeline_id FROM pipeline WHERE integrationid = v_integration_id AND identifier = 'whatsapp-cloud-webhook';
    IF v_pipeline_id IS NULL THEN
        RAISE EXCEPTION 'Pipeline whatsapp-cloud-webhook nao encontrada.';
    END IF;

    IF EXISTS (SELECT 1 FROM pipelinestep WHERE pipelineid = v_pipeline_id) THEN
        RAISE NOTICE 'Pipeline whatsapp-cloud-webhook ja possui steps. Abortando.';
        RETURN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM integrationattribute WHERE integrationid = v_integration_id AND field = 'callback_secret') THEN
        INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
        VALUES (v_integration_id, 'callback_secret', 'Secret do callback', 'IntegrationSecret do AgencyCampaign. Enviado no header X-Integration-Secret para autenticar o callback.', NULL, 1, NULL, true, 8, 'Webhook', true, now());
    END IF;

    INSERT INTO javascriptfunction (name, description, code, createdat) VALUES (
        'whatsapp-cloud-normalize-message',
        'Normaliza payload de mensagem recebida da WhatsApp Cloud API (Meta) para o formato padrão do inbox WhatsApp.',
$JS$
// Webhook da Meta envolve a mensagem em entry[].changes[].value.messages[]
var entry = payload.entry && payload.entry[0];
var value = entry && entry.changes && entry.changes[0] && entry.changes[0].value;
var messages = value && value.messages;
var m = messages && messages[0];

// Ignora eventos que nao sejam mensagens de texto recebidas (status updates, etc.)
if (!m || m.type !== 'text') {
    result.value = null;
} else {
    result.value = {
        from: m.from || '',
        message: (m.text && m.text.body) || '',
        timestamp: parseInt(m.timestamp) || Math.floor(Date.now() / 1000)
    };
}
$JS$,
        now()
    ) RETURNING id INTO v_jsfunc_id;

    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat) VALUES (
        'WhatsApp Cloud - Encaminhar mensagem ao AgencyCampaign',
        'POST target_callback_url com payload normalizado da mensagem recebida.',
        2,
        '{{ target_callback_url }}',
$HEADERS$
{
    "Content-Type": "application/json",
    "X-Integration-Secret": "{{ callback_secret }}"
}
$HEADERS$,
$BODY$
{
    "from": "{{ from }}",
    "message": "{{ message }}",
    "timestamp": {{ timestamp }}
}
$BODY$,
        now()
    ) RETURNING id INTO v_apicall_id;

    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES
        (v_pipeline_id, 1, 'Normalizar payload WhatsApp Cloud',  2, NULL,         v_jsfunc_id, NULL, 1, true, false, now()),
        (v_pipeline_id, 2, 'POST mensagem ao AgencyCampaign',    1, v_apicall_id, NULL,        NULL, 2, true, false, now());

    RAISE NOTICE 'whatsapp-cloud-webhook: steps criados (jsfunc=%, apicall=%)', v_jsfunc_id, v_apicall_id;
END $$;


-- =====================================================================
-- Twilio WhatsApp
-- =====================================================================
DO $$
DECLARE
    v_integration_id BIGINT;
    v_pipeline_id    BIGINT;
    v_jsfunc_id      BIGINT;
    v_apicall_id     BIGINT;
BEGIN
    SELECT id INTO v_integration_id FROM integration WHERE identifier = 'twilio-whatsapp';
    IF v_integration_id IS NULL THEN
        RAISE EXCEPTION 'Integration twilio-whatsapp nao encontrada.';
    END IF;

    SELECT id INTO v_pipeline_id FROM pipeline WHERE integrationid = v_integration_id AND identifier = 'twilio-whatsapp-webhook';
    IF v_pipeline_id IS NULL THEN
        RAISE EXCEPTION 'Pipeline twilio-whatsapp-webhook nao encontrada.';
    END IF;

    IF EXISTS (SELECT 1 FROM pipelinestep WHERE pipelineid = v_pipeline_id) THEN
        RAISE NOTICE 'Pipeline twilio-whatsapp-webhook ja possui steps. Abortando.';
        RETURN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM integrationattribute WHERE integrationid = v_integration_id AND field = 'callback_secret') THEN
        INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
        VALUES (v_integration_id, 'callback_secret', 'Secret do callback', 'IntegrationSecret do AgencyCampaign. Enviado no header X-Integration-Secret para autenticar o callback.', NULL, 1, NULL, true, 6, 'Webhook', true, now());
    END IF;

    INSERT INTO javascriptfunction (name, description, code, createdat) VALUES (
        'twilio-whatsapp-normalize-message',
        'Normaliza payload form-encoded do Twilio WhatsApp para o formato padrão do inbox WhatsApp.',
$JS$
// Twilio envia form-encoded; o ParseBody do IntegrationPlatform envolve como { "payload": "<raw>" }
var raw = payload.payload || '';
var params = {};
raw.split('&').forEach(function(pair) {
    var eq = pair.indexOf('=');
    if (eq > 0) {
        var k = decodeURIComponent(pair.substring(0, eq).replace(/\+/g, ' '));
        var v = decodeURIComponent(pair.substring(eq + 1).replace(/\+/g, ' '));
        params[k] = v;
    }
});

var from = (params['From'] || '').replace('whatsapp:', '').replace('+', '');
var message = params['Body'] || '';

if (!from || !message) {
    result.value = null;
} else {
    result.value = { from: from, message: message, timestamp: Math.floor(Date.now() / 1000) };
}
$JS$,
        now()
    ) RETURNING id INTO v_jsfunc_id;

    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat) VALUES (
        'Twilio WhatsApp - Encaminhar mensagem ao AgencyCampaign',
        'POST target_callback_url com payload normalizado da mensagem recebida.',
        2,
        '{{ target_callback_url }}',
$HEADERS$
{
    "Content-Type": "application/json",
    "X-Integration-Secret": "{{ callback_secret }}"
}
$HEADERS$,
$BODY$
{
    "from": "{{ from }}",
    "message": "{{ message }}",
    "timestamp": {{ timestamp }}
}
$BODY$,
        now()
    ) RETURNING id INTO v_apicall_id;

    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES
        (v_pipeline_id, 1, 'Normalizar payload Twilio WhatsApp', 2, NULL,         v_jsfunc_id, NULL, 1, true, false, now()),
        (v_pipeline_id, 2, 'POST mensagem ao AgencyCampaign',    1, v_apicall_id, NULL,        NULL, 2, true, false, now());

    RAISE NOTICE 'twilio-whatsapp-webhook: steps criados (jsfunc=%, apicall=%)', v_jsfunc_id, v_apicall_id;
END $$;


-- =====================================================================
-- 360dialog
-- =====================================================================
DO $$
DECLARE
    v_integration_id BIGINT;
    v_pipeline_id    BIGINT;
    v_jsfunc_id      BIGINT;
    v_apicall_id     BIGINT;
BEGIN
    SELECT id INTO v_integration_id FROM integration WHERE identifier = '360dialog';
    IF v_integration_id IS NULL THEN
        RAISE EXCEPTION 'Integration 360dialog nao encontrada.';
    END IF;

    SELECT id INTO v_pipeline_id FROM pipeline WHERE integrationid = v_integration_id AND identifier = '360dialog-webhook';
    IF v_pipeline_id IS NULL THEN
        RAISE EXCEPTION 'Pipeline 360dialog-webhook nao encontrada.';
    END IF;

    IF EXISTS (SELECT 1 FROM pipelinestep WHERE pipelineid = v_pipeline_id) THEN
        RAISE NOTICE 'Pipeline 360dialog-webhook ja possui steps. Abortando.';
        RETURN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM integrationattribute WHERE integrationid = v_integration_id AND field = 'callback_secret') THEN
        INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
        VALUES (v_integration_id, 'callback_secret', 'Secret do callback', 'IntegrationSecret do AgencyCampaign. Enviado no header X-Integration-Secret para autenticar o callback.', NULL, 1, NULL, true, 4, 'Webhook', true, now());
    END IF;

    INSERT INTO javascriptfunction (name, description, code, createdat) VALUES (
        '360dialog-normalize-message',
        'Normaliza payload do 360dialog (formato Meta WABA flat) para o formato padrão do inbox WhatsApp.',
$JS$
// 360dialog entrega o payload no formato WABA flat (sem o envelope entry[].changes[])
var messages = payload.messages;
var m = messages && messages[0];

if (!m || m.type !== 'text') {
    result.value = null;
} else {
    result.value = {
        from: m.from || '',
        message: (m.text && m.text.body) || '',
        timestamp: parseInt(m.timestamp) || Math.floor(Date.now() / 1000)
    };
}
$JS$,
        now()
    ) RETURNING id INTO v_jsfunc_id;

    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat) VALUES (
        '360dialog - Encaminhar mensagem ao AgencyCampaign',
        'POST target_callback_url com payload normalizado da mensagem recebida.',
        2,
        '{{ target_callback_url }}',
$HEADERS$
{
    "Content-Type": "application/json",
    "X-Integration-Secret": "{{ callback_secret }}"
}
$HEADERS$,
$BODY$
{
    "from": "{{ from }}",
    "message": "{{ message }}",
    "timestamp": {{ timestamp }}
}
$BODY$,
        now()
    ) RETURNING id INTO v_apicall_id;

    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES
        (v_pipeline_id, 1, 'Normalizar payload 360dialog',       2, NULL,         v_jsfunc_id, NULL, 1, true, false, now()),
        (v_pipeline_id, 2, 'POST mensagem ao AgencyCampaign',    1, v_apicall_id, NULL,        NULL, 2, true, false, now());

    RAISE NOTICE '360dialog-webhook: steps criados (jsfunc=%, apicall=%)', v_jsfunc_id, v_apicall_id;
END $$;


-- =====================================================================
-- DOWN (reversao manual - descomentar e executar para remover tudo)
-- =====================================================================
-- DO $$
-- DECLARE
--     v_pipeline_ids BIGINT[];
-- BEGIN
--     SELECT array_agg(p.id) INTO v_pipeline_ids
--     FROM pipeline p
--     JOIN integration i ON i.id = p.integrationid
--     WHERE (i.identifier, p.identifier) IN (
--         ('z-api',          'z-api-webhook'),
--         ('evolution-api',  'evolution-webhook'),
--         ('whatsapp-cloud', 'whatsapp-cloud-webhook'),
--         ('twilio-whatsapp','twilio-whatsapp-webhook'),
--         ('360dialog',      '360dialog-webhook')
--     );
--
--     DELETE FROM pipelinestep WHERE pipelineid = ANY(v_pipeline_ids);
--
--     DELETE FROM apicall WHERE name IN (
--         'Z-API - Encaminhar mensagem ao AgencyCampaign',
--         'Evolution API - Encaminhar mensagem ao AgencyCampaign',
--         'WhatsApp Cloud - Encaminhar mensagem ao AgencyCampaign',
--         'Twilio WhatsApp - Encaminhar mensagem ao AgencyCampaign',
--         '360dialog - Encaminhar mensagem ao AgencyCampaign'
--     );
--
--     DELETE FROM javascriptfunction WHERE name IN (
--         'z-api-normalize-message',
--         'evolution-api-normalize-message',
--         'whatsapp-cloud-normalize-message',
--         'twilio-whatsapp-normalize-message',
--         '360dialog-normalize-message'
--     );
--
--     DELETE FROM integrationattribute
--     WHERE field = 'callback_secret'
--     AND integrationid IN (
--         SELECT id FROM integration
--         WHERE identifier IN ('z-api','evolution-api','whatsapp-cloud','twilio-whatsapp','360dialog')
--     );
-- END $$;
