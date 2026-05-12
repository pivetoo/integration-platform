-- =====================================================================
-- Seed: Integração AWS SES no IntegrationPlatform
--
-- Cria categoria "E-mail" + Integration "aws-ses" + atributos +
-- 1 JavaScriptFunction (sign request SigV4 + monta body SES v2) +
-- 1 ApiCall (POST /v2/email/outbound-emails) +
-- 1 Pipeline (aws-ses-send-email outbound)
--
-- IDEMPOTENTE: aborta se ja existir Integration com identifier='aws-ses'
--
-- Payload esperado pelo AgencyCampaign no enqueue:
--   {
--     "to": "destinatario@example.com",
--     "toName": "Nome Destinatario",          (opcional)
--     "cc": ["cc@example.com"],               (opcional)
--     "subject": "Assunto do e-mail",
--     "htmlBody": "<h1>Conteudo HTML</h1>",
--     "textBody": "Conteudo texto plano",     (opcional, gerado do html se ausente)
--     "replyTo": "reply@example.com"          (opcional)
--   }
--
-- Nota sobre autenticacao:
--   O AWS SES v2 exige AWS Signature Version 4 (SigV4). Como o header
--   Authorization muda a cada request (inclui timestamp + HMAC-SHA256),
--   ele nao pode ser um valor estatico no connector. A JavaScriptFunction
--   implementa SHA-256 e HMAC-SHA256 em pure JS (sem crypto.subtle,
--   compativel com Jint) e computa o header dinamicamente.
-- =====================================================================

DO $$
DECLARE
    v_category_id    BIGINT;
    v_integration_id BIGINT;
    v_jsfunc_sign    BIGINT;
    v_apicall_send   BIGINT;
    v_pipeline_send  BIGINT;
BEGIN
    IF EXISTS (SELECT 1 FROM integration WHERE identifier = 'aws-ses') THEN
        RAISE NOTICE 'AWS SES integration ja existe. Abortando seed.';
        RETURN;
    END IF;

    -- 1) Categoria "E-mail"
    SELECT id INTO v_category_id
    FROM integrationcategory
    WHERE LOWER(name) = LOWER('E-mail')
    LIMIT 1;

    IF v_category_id IS NULL THEN
        INSERT INTO integrationcategory (name, description, isactive, createdat)
        VALUES (
            'E-mail',
            'Provedores de envio de e-mail transacional e marketing.',
            true,
            now()
        )
        RETURNING id INTO v_category_id;
        RAISE NOTICE 'Categoria E-mail criada id=%', v_category_id;
    ELSE
        RAISE NOTICE 'Categoria E-mail existente reutilizada id=%', v_category_id;
    END IF;

    -- 2) Integration "AWS SES"
    INSERT INTO integration (identifier, name, description, integrationcategoryid, isactive, createdat)
    VALUES (
        'aws-ses',
        'AWS SES',
        'Envio de e-mail transacional via Amazon Simple Email Service v2. Requer dominio verificado na conta AWS e credenciais IAM com permissao ses:SendEmail.',
        v_category_id,
        true,
        now()
    )
    RETURNING id INTO v_integration_id;
    RAISE NOTICE 'Integration aws-ses criada id=%', v_integration_id;

    -- 3) Atributos de configuracao do connector
    -- type: 1=Text
    INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
    VALUES
      (v_integration_id, 'aws_access_key_id',     'AWS Access Key ID',
       'ID da chave de acesso IAM com permissao ses:SendEmail.',
       'AKIAIOSFODNN7EXAMPLE',
       1, NULL, true, 1, 'Autenticacao', false, now()),

      (v_integration_id, 'aws_secret_access_key', 'AWS Secret Access Key',
       'Chave secreta correspondente ao Access Key ID. Nunca exposta na UI.',
       NULL,
       1, NULL, true, 2, 'Autenticacao', true, now()),

      (v_integration_id, 'aws_region',            'Regiao AWS',
       'Regiao onde o SES esta configurado. Ex: us-east-1, sa-east-1, eu-west-1.',
       'us-east-1',
       1, 'us-east-1', true, 3, 'Configuracao', false, now()),

      (v_integration_id, 'from_email',            'E-mail remetente',
       'Endereco de e-mail verificado no SES usado como remetente. Ex: noreply@seudominio.com.',
       'noreply@seudominio.com',
       1, NULL, true, 4, 'Configuracao', false, now()),

      (v_integration_id, 'from_name',             'Nome do remetente',
       'Nome exibido no campo "De:" do e-mail. Opcional.',
       'Mainstay',
       1, NULL, false, 5, 'Configuracao', false, now());

    -- 4) JavaScriptFunction: computa SigV4 + monta body SES v2
    INSERT INTO javascriptfunction (name, description, code, createdat)
    VALUES (
        'aws-ses-sign-request',
        'Monta o body JSON para SES v2 e computa o header Authorization AWS Signature V4 (SHA-256 + HMAC em pure JS, sem crypto.subtle).',
$JS$
// ── Entrada ──────────────────────────────────────────────────────────────────
// payload:    { to, toName?, cc?, subject, htmlBody, textBody?, replyTo? }
// attributes: { aws_access_key_id, aws_secret_access_key, aws_region,
//               from_email, from_name? }
//
// ── Saida (result.value) ─────────────────────────────────────────────────────
// ses_body          : string JSON do request body SES v2
// ses_authorization : header Authorization (AWS4-HMAC-SHA256 ...)
// ses_amz_date      : header X-Amz-Date (ex: 20260512T153000Z)
// ses_content_hash  : header X-Amz-Content-Sha256 (hex do SHA-256 do body)
// ses_host          : header Host (ex: email.us-east-1.amazonaws.com)

// =============================================================================
// SHA-256 em pure JS (sem browser crypto)
// Entrada: array de bytes (inteiros 0-255)
// Saida: string hex de 64 chars
// =============================================================================
function sha256hex(inputBytes) {
    var K = [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
        0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
        0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
        0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
        0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
        0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
        0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
        0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
        0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    ];
    var iv = [
        0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a,
        0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19
    ];

    function rotr(x, n) { return (x >>> n) | (x << (32 - n)); }

    var bytes = inputBytes.slice();
    var bitLen = bytes.length * 8;
    bytes.push(0x80);
    while ((bytes.length % 64) !== 56) { bytes.push(0x00); }
    bytes.push(0, 0, 0, 0,
        (bitLen >>> 24) & 0xFF, (bitLen >>> 16) & 0xFF,
        (bitLen >>> 8)  & 0xFF,  bitLen          & 0xFF);

    var h = iv.slice();
    var blk, wi, w, a, b, c, d, e, f, g, hv, s0, s1, ch, maj, t1, t2;

    for (blk = 0; blk < bytes.length; blk += 64) {
        w = [];
        for (wi = 0; wi < 16; wi++) {
            w[wi] = (bytes[blk + wi*4]     << 24) |
                    (bytes[blk + wi*4 + 1] << 16) |
                    (bytes[blk + wi*4 + 2] <<  8) |
                     bytes[blk + wi*4 + 3];
        }
        for (wi = 16; wi < 64; wi++) {
            s0  = rotr(w[wi-15],  7) ^ rotr(w[wi-15], 18) ^ (w[wi-15] >>>  3);
            s1  = rotr(w[wi- 2], 17) ^ rotr(w[wi- 2], 19) ^ (w[wi- 2] >>> 10);
            w[wi] = (w[wi-16] + s0 + w[wi-7] + s1) | 0;
        }

        a = h[0]; b = h[1]; c = h[2]; d = h[3];
        e = h[4]; f = h[5]; g = h[6]; hv = h[7];

        for (wi = 0; wi < 64; wi++) {
            s1  = rotr(e,  6) ^ rotr(e, 11) ^ rotr(e, 25);
            ch  = (e & f) ^ (~e & g);
            t1  = (hv + s1 + ch + K[wi] + w[wi]) | 0;
            s0  = rotr(a,  2) ^ rotr(a, 13) ^ rotr(a, 22);
            maj = (a & b) ^ (a & c) ^ (b & c);
            t2  = (s0 + maj) | 0;

            hv = g; g = f; f = e; e = (d  + t1) | 0;
            d  = c; c = b; b = a; a = (t1 + t2) | 0;
        }

        h[0] = (h[0]+a)|0; h[1] = (h[1]+b)|0;
        h[2] = (h[2]+c)|0; h[3] = (h[3]+d)|0;
        h[4] = (h[4]+e)|0; h[5] = (h[5]+f)|0;
        h[6] = (h[6]+g)|0; h[7] = (h[7]+hv)|0;
    }

    var hex = '';
    for (var hi = 0; hi < 8; hi++) {
        hex += ('0000000' + ((h[hi] >>> 0).toString(16))).slice(-8);
    }
    return hex;
}

// Converte string UTF-8 para array de bytes
function strToBytes(s) {
    var b = [], c, i;
    for (i = 0; i < s.length; i++) {
        c = s.charCodeAt(i);
        if (c < 0x80) {
            b.push(c);
        } else if (c < 0x800) {
            b.push(0xC0 | (c >> 6));
            b.push(0x80 | (c & 0x3F));
        } else {
            b.push(0xE0 | (c >> 12));
            b.push(0x80 | ((c >> 6) & 0x3F));
            b.push(0x80 | (c & 0x3F));
        }
    }
    return b;
}

function hexToBytes(hex) {
    var b = [];
    for (var i = 0; i < hex.length; i += 2) {
        b.push(parseInt(hex.substr(i, 2), 16));
    }
    return b;
}

function bytesToHex(b) {
    var h = '';
    for (var i = 0; i < b.length; i++) {
        h += ('0' + b[i].toString(16)).slice(-2);
    }
    return h;
}

// HMAC-SHA256: key e data sao arrays de bytes; retorna array de bytes
function hmacBytes(keyBytes, dataBytes) {
    var BLOCK = 64, i;
    var key = keyBytes.slice();
    if (key.length > BLOCK) { key = hexToBytes(sha256hex(key)); }
    while (key.length < BLOCK) { key.push(0); }
    var ipad = [], opad = [];
    for (i = 0; i < BLOCK; i++) {
        ipad.push(key[i] ^ 0x36);
        opad.push(key[i] ^ 0x5C);
    }
    var inner = hexToBytes(sha256hex(ipad.concat(dataBytes)));
    return hexToBytes(sha256hex(opad.concat(inner)));
}

function hmacHex(keyBytes, dataStr) {
    return bytesToHex(hmacBytes(keyBytes, strToBytes(dataStr)));
}

// =============================================================================
// Monta o request body SES v2
// =============================================================================
var p   = payload    || {};
var att = attributes || {};

var toList = [];
if (typeof p.to === 'string') {
    toList = [p.to];
} else if (Array.isArray(p.to)) {
    toList = p.to;
}

var ccList = Array.isArray(p.cc) ? p.cc : (p.cc ? [p.cc] : []);

var fromAddr = att.from_name
    ? '"' + att.from_name + '" <' + att.from_email + '>'
    : att.from_email;

var textFallback = (p.htmlBody || '').replace(/<[^>]+>/g, '').replace(/\s+/g, ' ').trim();

var sesBody = {
    FromEmailAddress: fromAddr,
    Destination: { ToAddresses: toList },
    Content: {
        Simple: {
            Subject: { Data: p.subject || '', Charset: 'UTF-8' },
            Body: {
                Html: { Data: p.htmlBody || '',           Charset: 'UTF-8' },
                Text: { Data: p.textBody || textFallback, Charset: 'UTF-8' }
            }
        }
    }
};
if (ccList.length > 0)  { sesBody.Destination.CcAddresses = ccList; }
if (p.replyTo)          { sesBody.ReplyToAddresses = [p.replyTo]; }

var bodyStr  = JSON.stringify(sesBody);
var bodyHash = sha256hex(strToBytes(bodyStr));

// =============================================================================
// AWS Signature V4
// =============================================================================
var accessKeyId = att.aws_access_key_id     || '';
var secretKey   = att.aws_secret_access_key || '';
var region      = att.aws_region            || 'us-east-1';
var service     = 'ses';
var host        = 'email.' + region + '.amazonaws.com';

// Timestamp no formato exigido pelo SigV4
var now = new Date();
var pad = function(n) { return n < 10 ? '0' + n : '' + n; };
var dateStamp = now.getUTCFullYear().toString()
    + pad(now.getUTCMonth() + 1)
    + pad(now.getUTCDate());
var amzDate = dateStamp
    + 'T' + pad(now.getUTCHours())
    + pad(now.getUTCMinutes())
    + pad(now.getUTCSeconds()) + 'Z';

// Canonical request
// Headers assinados: content-type, host, x-amz-content-sha256, x-amz-date (ordem alfabetica)
var signedHeaders = 'content-type;host;x-amz-content-sha256;x-amz-date';
var canonicalHeaders =
    'content-type:application/json\n' +
    'host:' + host + '\n' +
    'x-amz-content-sha256:' + bodyHash + '\n' +
    'x-amz-date:' + amzDate + '\n';

var canonicalRequest = [
    'POST',
    '/v2/email/outbound-emails',
    '',
    canonicalHeaders,
    signedHeaders,
    bodyHash
].join('\n');

// String to sign
var credScope  = dateStamp + '/' + region + '/ses/aws4_request';
var strToSign  = 'AWS4-HMAC-SHA256\n' + amzDate + '\n' + credScope + '\n'
    + sha256hex(strToBytes(canonicalRequest));

// Derivacao da chave de assinatura
var kDate    = hmacBytes(strToBytes('AWS4' + secretKey), dateStamp);
var kRegion  = hmacBytes(kDate,    region);
var kService = hmacBytes(kRegion,  service);
var kSigning = hmacBytes(kService, 'aws4_request');
var signature = hmacHex(kSigning, strToSign);

// Header Authorization final
var authHeader = 'AWS4-HMAC-SHA256 Credential=' + accessKeyId + '/' + credScope
    + ', SignedHeaders=' + signedHeaders
    + ', Signature=' + signature;

result.value = {
    ses_body:          bodyStr,
    ses_authorization: authHeader,
    ses_amz_date:      amzDate,
    ses_content_hash:  bodyHash,
    ses_host:          host
};
$JS$,
        now()
    )
    RETURNING id INTO v_jsfunc_sign;

    -- 5) ApiCall: POST /v2/email/outbound-emails
    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat)
    VALUES (
        'AWS SES - Enviar e-mail',
        'POST /v2/email/outbound-emails — envia e-mail transacional via SES v2 com autenticacao AWS Signature V4.',
        2, -- Post
        'https://{{ ses_host }}/v2/email/outbound-emails',
$HEADERS$
{
    "Authorization": "{{ ses_authorization }}",
    "X-Amz-Date": "{{ ses_amz_date }}",
    "X-Amz-Content-Sha256": "{{ ses_content_hash }}",
    "Content-Type": "application/json"
}
$HEADERS$,
$BODY$
{{ ses_body }}
$BODY$,
        now()
    )
    RETURNING id INTO v_apicall_send;

    -- 6) Pipeline outbound: aws-ses-send-email
    INSERT INTO pipeline (integrationid, identifier, name, description, isactive, createdat)
    VALUES (
        v_integration_id,
        'aws-ses-send-email',
        'Enviar e-mail',
        'Pipeline acionada via enqueue com { to, subject, htmlBody, ... }. Computa SigV4 e envia via SES v2.',
        true,
        now()
    )
    RETURNING id INTO v_pipeline_send;

    -- 7) Steps do pipeline
    -- type: 1=HttpRequest, 2=JavaScriptFunction
    -- erroraction: 1=Stop, 2=Continue
    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES
      (v_pipeline_send, 1, 'Assinar request SigV4 e montar body SES', 2, NULL, v_jsfunc_sign, NULL, 1, true, false, now()),
      (v_pipeline_send, 2, 'POST /v2/email/outbound-emails',           1, v_apicall_send, NULL, NULL, 1, true, false, now());

    RAISE NOTICE 'Seed AWS SES concluida. integration=%, pipeline=%, jsfunc=%, apicall=%',
        v_integration_id, v_pipeline_send, v_jsfunc_sign, v_apicall_send;
END $$;

-- =====================================================================
-- DOWN (reversao manual)
-- =====================================================================
-- DO $$
-- DECLARE v_integration_id BIGINT;
-- BEGIN
--     SELECT id INTO v_integration_id FROM integration WHERE identifier = 'aws-ses';
--     IF v_integration_id IS NULL THEN RETURN; END IF;
--
--     DELETE FROM pipelinestep WHERE pipelineid IN (
--         SELECT id FROM pipeline WHERE integrationid = v_integration_id
--     );
--     DELETE FROM pipeline       WHERE integrationid = v_integration_id;
--     DELETE FROM apicall        WHERE name = 'AWS SES - Enviar e-mail';
--     DELETE FROM javascriptfunction WHERE name = 'aws-ses-sign-request';
--     DELETE FROM connectorattributevalue WHERE integrationattributeid IN (
--         SELECT id FROM integrationattribute WHERE integrationid = v_integration_id
--     );
--     DELETE FROM integrationattribute WHERE integrationid = v_integration_id;
--     DELETE FROM connector    WHERE integrationid = v_integration_id;
--     DELETE FROM integration  WHERE id = v_integration_id;
-- END $$;
