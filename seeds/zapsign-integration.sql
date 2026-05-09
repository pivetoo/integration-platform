-- =====================================================================
-- Seed: Integração ZapSign no IntegrationPlatform
--
-- Cria categoria "Assinatura Digital" + Integration "ZapSign" + 3 atributos +
-- 3 ApiCalls + 2 Pipelines (Send / Cancel) + steps.
--
-- IDEMPOTENTE: aborta se já existir Integration com identifier='zapsign'.
-- Reverter manualmente: ver bloco DOWN no final (comentado).
--
-- Convencao do payload enviado pelo AgencyCampaign no enqueue:
--   {
--     "campaignDocumentId": 123,
--     "title": "Contrato ...",
--     "body": "...",                  -- opcional, nao usado no MVP
--     "documentUrl": "https://...",   -- obrigatorio: URL publica do PDF
--     "signers": [ { "role": "Creator", "name": "...", "email": "...", "documentNumber": "..." }, ... ]
--   }
--
-- Atributos esperados no Connector (configurados pelo usuario na tela):
--   - api_token   (sensitive)  -> Bearer da conta ZapSign
--   - base_url                 -> https://api.zapsign.com.br (sandbox: https://sandbox.api.zapsign.com.br)
--   - lang                     -> pt-br
-- =====================================================================

DO $$
DECLARE
    v_category_id     BIGINT;
    v_integration_id  BIGINT;
    v_apicall_create  BIGINT;
    v_apicall_cancel  BIGINT;
    v_apicall_get     BIGINT;
    v_pipeline_send   BIGINT;
    v_pipeline_cancel BIGINT;
BEGIN
    IF EXISTS (SELECT 1 FROM integration WHERE identifier = 'zapsign') THEN
        RAISE NOTICE 'ZapSign integration ja existe. Abortando seed.';
        RETURN;
    END IF;

    -- 1) Categoria "Assinatura Digital" (cria se nao existir)
    SELECT id INTO v_category_id
    FROM integrationcategory
    WHERE LOWER(name) = LOWER('Assinatura Digital')
    LIMIT 1;

    IF v_category_id IS NULL THEN
        INSERT INTO integrationcategory (name, description, isactive, createdat)
        VALUES ('Assinatura Digital', 'Provedores de coleta de assinatura eletronica de documentos.', true, now())
        RETURNING id INTO v_category_id;
        RAISE NOTICE 'Categoria criada id=%', v_category_id;
    ELSE
        RAISE NOTICE 'Categoria existente reutilizada id=%', v_category_id;
    END IF;

    -- 2) Integration "ZapSign"
    INSERT INTO integration (identifier, name, description, integrationcategoryid, isactive, createdat)
    VALUES (
        'zapsign',
        'ZapSign',
        'Coleta de assinatura digital de documentos via ZapSign (https://zapsign.com.br).',
        v_category_id,
        true,
        now()
    )
    RETURNING id INTO v_integration_id;
    RAISE NOTICE 'Integration zapsign criada id=%', v_integration_id;

    -- 3) Attributes do conector (schema da configuracao)
    -- type 1 = Text, type 2 = LongText
    INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, "order", "group", issensitive, createdat)
    VALUES
      (v_integration_id, 'api_token', 'API Token',
       'Token Bearer da conta ZapSign. Obtido em Configuracoes > API.',
       'c7f35c84-7893-4087-...', 1, NULL, true, 1, 'Autenticacao', true, now()),
      (v_integration_id, 'base_url', 'URL base da API',
       'Endpoint base da API. Use sandbox para testes.',
       'https://api.zapsign.com.br', 1, 'https://api.zapsign.com.br', true, 2, 'Endpoints', false, now()),
      (v_integration_id, 'lang', 'Idioma padrao',
       'Idioma usado nos emails e modal de assinatura. Valores aceitos: pt-br, es, en, fr.',
       'pt-br', 1, 'pt-br', false, 3, 'Endpoints', false, now());

    -- 4) ApiCalls (definicoes HTTP reutilizaveis)
    -- 4.1) Criar documento - POST /api/v1/docs/
    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat)
    VALUES (
        'ZapSign - Criar documento',
        'POST /api/v1/docs/ - cria documento e dispara emails de assinatura aos signers.',
        2, -- Post
        '{{ base_url }}/api/v1/docs/',
$HEADERS$
{
    "Authorization": "Bearer {{ api_token }}",
    "Content-Type": "application/json"
}
$HEADERS$,
$BODY$
{
    "name": "{{ title }}",
    "url_pdf": "{{ documentUrl }}",
    "signers": {{ signers }},
    "lang": "{{ lang }}",
    "external_id": "{{ campaignDocumentId }}",
    "send_automatic_email": true
}
$BODY$,
        now()
    )
    RETURNING id INTO v_apicall_create;

    -- 4.2) Cancelar documento - DELETE /api/v1/docs/{token}/
    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat)
    VALUES (
        'ZapSign - Cancelar documento',
        'DELETE /api/v1/docs/{token}/ - cancela um documento ainda nao assinado.',
        5, -- Delete
        '{{ base_url }}/api/v1/docs/{{ providerDocumentId }}/',
$HEADERS$
{
    "Authorization": "Bearer {{ api_token }}",
    "Content-Type": "application/json"
}
$HEADERS$,
        NULL,
        now()
    )
    RETURNING id INTO v_apicall_cancel;

    -- 4.3) Consultar status - GET /api/v1/docs/{token}/
    INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat)
    VALUES (
        'ZapSign - Consultar documento',
        'GET /api/v1/docs/{token}/ - consulta o status atual de um documento.',
        1, -- Get
        '{{ base_url }}/api/v1/docs/{{ providerDocumentId }}/',
$HEADERS$
{
    "Authorization": "Bearer {{ api_token }}"
}
$HEADERS$,
        NULL,
        now()
    )
    RETURNING id INTO v_apicall_get;

    -- 5) Pipelines
    -- 5.1) Enviar para assinatura
    INSERT INTO pipeline (integrationid, identifier, name, description, isactive, createdat)
    VALUES (
        v_integration_id,
        'zapsign-send-document',
        'Enviar documento para assinatura',
        'Cria um documento na ZapSign e dispara emails de assinatura aos signatarios.',
        true,
        now()
    )
    RETURNING id INTO v_pipeline_send;

    -- 5.2) Cancelar documento
    INSERT INTO pipeline (integrationid, identifier, name, description, isactive, createdat)
    VALUES (
        v_integration_id,
        'zapsign-cancel-document',
        'Cancelar documento',
        'Cancela um documento ainda nao assinado.',
        true,
        now()
    )
    RETURNING id INTO v_pipeline_cancel;

    -- 6) Pipeline Steps
    -- erroraction: 1=Stop, 2=Continue
    -- type: 1=HttpRequest, 2=JavaScriptFunction, 3=ExecuteScript

    -- Step do Send: chama ApiCall criar documento
    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES (
        v_pipeline_send,
        1,
        'POST /api/v1/docs/',
        1,
        v_apicall_create,
        NULL,
        NULL,
        1,
        true,
        false,
        now()
    );

    -- Step do Cancel: chama ApiCall cancelar
    INSERT INTO pipelinestep (pipelineid, "order", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat)
    VALUES (
        v_pipeline_cancel,
        1,
        'DELETE /api/v1/docs/{token}/',
        1,
        v_apicall_cancel,
        NULL,
        NULL,
        1,
        true,
        false,
        now()
    );

    RAISE NOTICE 'Seed ZapSign concluida com sucesso. integration_id=%, pipelines=[%, %], apicalls=[%, %, %]',
        v_integration_id, v_pipeline_send, v_pipeline_cancel, v_apicall_create, v_apicall_cancel, v_apicall_get;
END $$;

-- =====================================================================
-- DOWN (reversao manual - descomentar e executar se precisar remover)
-- =====================================================================
-- DO $$
-- DECLARE
--     v_integration_id BIGINT;
-- BEGIN
--     SELECT id INTO v_integration_id FROM integration WHERE identifier = 'zapsign';
--     IF v_integration_id IS NULL THEN
--         RAISE NOTICE 'ZapSign integration nao encontrada. Nada a remover.';
--         RETURN;
--     END IF;
--
--     DELETE FROM pipelinestepvaluemapping WHERE pipelinestepid IN (
--         SELECT id FROM pipelinestep WHERE pipelineid IN (SELECT id FROM pipeline WHERE integrationid = v_integration_id)
--     );
--     DELETE FROM pipelinestep WHERE pipelineid IN (SELECT id FROM pipeline WHERE integrationid = v_integration_id);
--     DELETE FROM apicall WHERE id IN (
--         SELECT DISTINCT apicallid FROM pipelinestep WHERE apicallid IS NOT NULL AND pipelineid IN (
--             SELECT id FROM pipeline WHERE integrationid = v_integration_id
--         )
--     );
--     DELETE FROM apicall WHERE name LIKE 'ZapSign - %';
--     DELETE FROM pipeline WHERE integrationid = v_integration_id;
--     DELETE FROM connectorattributevalue WHERE integrationattributeid IN (
--         SELECT id FROM integrationattribute WHERE integrationid = v_integration_id
--     );
--     DELETE FROM integrationattribute WHERE integrationid = v_integration_id;
--     DELETE FROM connector WHERE integrationid = v_integration_id;
--     DELETE FROM integration WHERE id = v_integration_id;
--
--     -- Categoria "Assinatura Digital" so e removida se nao houver outras integrations nela:
--     DELETE FROM integrationcategory
--     WHERE LOWER(name) = LOWER('Assinatura Digital')
--       AND NOT EXISTS (SELECT 1 FROM integration WHERE integrationcategoryid = integrationcategory.id);
-- END $$;
