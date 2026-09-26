using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations
{
    // Base reutilizavel para os seeds de integracao por modulo (Email, Whatsapp, etc.).
    // Todos os helpers emitem INSERT idempotente (WHERE NOT EXISTS) e resolvem FK por chave
    // natural (identifier/name), nunca por id, para convergir com tenants existentes e criar
    // limpo em tenant novo. Sao seeds de dado condicional: SQL e a forma correta aqui (a API
    // fluente do FluentMigrator nao expressa WHERE NOT EXISTS nem resolucao de FK por subquery).
    public abstract class IntegrationSeedMigration : Migration
    {
        protected void SeedCategory(string identifier, string name, string description)
        {
            Execute.Sql($@"
                INSERT INTO integrationcategory (identifier, name, description, isactive, createdat, updatedat)
                SELECT {Txt(identifier)}, {Txt(name)}, {Nullable(description)}, true, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = {Txt(identifier)});
            ");
        }

        protected void SeedContract(string identifier, string name, string description, string categoryIdentifier, string inputSchema, string outputSchema, bool hasCallback, string? callbackSchema, bool includeOutputInCallback = false)
        {
            Execute.Sql($@"
                INSERT INTO servicecontract (identifier, name, description, integrationcategoryid, inputschema, outputschema, hascallback, callbackschema, isactive, includeoutputincallback, createdat, updatedat)
                SELECT {Txt(identifier)}, {Txt(name)}, {Nullable(description)}, {CategoryId(categoryIdentifier)}, {Nullable(inputSchema)}, {Nullable(outputSchema)}, {Bool(hasCallback)}, {Nullable(callbackSchema)}, true, {Bool(includeOutputInCallback)}, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM servicecontract WHERE identifier = {Txt(identifier)});
            ");
        }

        protected void SeedIntegration(string identifier, string name, string description, string categoryIdentifier, string? iconUrl, bool supportsWebhook)
        {
            Execute.Sql($@"
                INSERT INTO integration (identifier, name, description, integrationcategoryid, iconurl, supportswebhook, isactive, createdat, updatedat)
                SELECT {Txt(identifier)}, {Txt(name)}, {Nullable(description)}, {CategoryId(categoryIdentifier)}, {Nullable(iconUrl)}, {Bool(supportsWebhook)}, true, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM integration WHERE identifier = {Txt(identifier)});
            ");
        }

        protected void SeedAttribute(string integration, string field, string label, FieldType type, bool required, int order, string group, bool sensitive = false, bool hidden = false, string? description = null, string? placeholder = null, string? defaultValue = null)
        {
            Execute.Sql($@"
                INSERT INTO integrationattribute (integrationid, field, label, description, placeholder, type, defaultvalue, isrequired, ""order"", ""group"", issensitive, ishidden, createdat, updatedat)
                SELECT {IntegrationId(integration)}, {Txt(field)}, {Txt(label)}, {Nullable(description)}, {Nullable(placeholder)}, {(int)type}, {Nullable(defaultValue)}, {Bool(required)}, {order}, {Nullable(group)}, {Bool(sensitive)}, {Bool(hidden)}, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM integrationattribute WHERE integrationid = {IntegrationId(integration)} AND field = {Txt(field)});
            ");
        }

        protected void BindContract(string integration, string contractIdentifier)
        {
            Execute.Sql($@"
                INSERT INTO integrationservicecontract (integrationid, servicecontractid, isactive, createdat, updatedat)
                SELECT {IntegrationId(integration)}, {ContractId(contractIdentifier)}, true, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM integrationservicecontract WHERE integrationid = {IntegrationId(integration)} AND servicecontractid = {ContractId(contractIdentifier)});
            ");
        }

        protected void SeedApiCall(string name, HttpMethodType method, string url, string headersTemplate, string bodyTemplate, string? description = null)
        {
            Execute.Sql($@"
                INSERT INTO apicall (name, description, method, url, headerstemplate, bodytemplate, createdat, updatedat)
                SELECT {Txt(name)}, {Nullable(description)}, {(int)method}, {Txt(url)}, {Nullable(headersTemplate)}, {Nullable(bodyTemplate)}, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM apicall WHERE name = {Txt(name)});
            ");
        }

        protected void SeedJsFunction(string name, string code, string? description = null)
        {
            Execute.Sql($@"
                INSERT INTO javascriptfunction (name, description, code, createdat, updatedat)
                SELECT {Txt(name)}, {Nullable(description)}, {Txt(code)}, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM javascriptfunction WHERE name = {Txt(name)});
            ");
        }

        protected void SeedPipeline(string integration, string identifier, string name, string description, bool isDefault, bool isTestPipeline, string? contractIdentifier)
        {
            string contract = contractIdentifier is null ? "NULL" : ContractId(contractIdentifier);
            Execute.Sql($@"
                INSERT INTO pipeline (integrationid, identifier, name, description, isactive, isdefault, istestpipeline, servicecontractid, createdat, updatedat)
                SELECT {IntegrationId(integration)}, {Txt(identifier)}, {Txt(name)}, {Nullable(description)}, true, {Bool(isDefault)}, {Bool(isTestPipeline)}, {contract}, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM pipeline WHERE identifier = {Txt(identifier)});
            ");
        }

        protected void SeedStep(string pipeline, int order, string name, PipelineStepType type, ErrorAction errorAction, string? apiCall = null, string? jsFunction = null, bool ignoreOnResponse = false)
        {
            string apiCallId = apiCall is null ? "NULL" : $"(SELECT id FROM apicall WHERE name = {Txt(apiCall)})";
            string jsFunctionId = jsFunction is null ? "NULL" : $"(SELECT id FROM javascriptfunction WHERE name = {Txt(jsFunction)})";
            Execute.Sql($@"
                INSERT INTO pipelinestep (pipelineid, ""order"", name, type, apicallid, javascriptfunctionid, databasescriptid, erroraction, isactive, ignoreonresponse, createdat, updatedat)
                SELECT {PipelineId(pipeline)}, {order}, {Txt(name)}, {(int)type}, {apiCallId}, {jsFunctionId}, NULL, {(int)errorAction}, true, {Bool(ignoreOnResponse)}, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM pipelinestep WHERE pipelineid = {PipelineId(pipeline)} AND ""order"" = {order});
            ");
        }

        private static string CategoryId(string identifier) => $"(SELECT id FROM integrationcategory WHERE identifier = {Txt(identifier)})";

        private static string IntegrationId(string identifier) => $"(SELECT id FROM integration WHERE identifier = {Txt(identifier)})";

        private static string ContractId(string identifier) => $"(SELECT id FROM servicecontract WHERE identifier = {Txt(identifier)})";

        private static string PipelineId(string identifier) => $"(SELECT id FROM pipeline WHERE identifier = {Txt(identifier)})";

        private static string Txt(string value) => "'" + value.Replace("'", "''") + "'";

        private static string Nullable(string? value) => value is null ? "NULL" : Txt(value);

        private static string Bool(bool value) => value ? "true" : "false";
    }
}
