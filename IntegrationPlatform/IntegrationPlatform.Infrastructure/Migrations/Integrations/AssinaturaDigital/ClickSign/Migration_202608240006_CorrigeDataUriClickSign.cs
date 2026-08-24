using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.ClickSign
{
    // ClickSign v3 exige que content_base64 (POST /envelopes/{id}/documents) seja um Data URI
    // completo (RFC 2397: "data:<mime>;base64,<dados>"), nao o base64 puro que outros provedores
    // (ex.: ZapSign) aceitam. Testado ao vivo em sandbox: sem o prefixo, a API devolve 400
    // "content_base64 Formatação do campo inválida. O valor deve ser um Data URI completo.".
    [Migration(202608240006)]
    public sealed class Migration_202608240006_CorrigeDataUriClickSign : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE javascriptfunction SET
                    code = replace(code, 'out.documentBase64=s(payload.documentBase64);', 'var rawBase64=s(payload.documentBase64);out.documentBase64=rawBase64.indexOf(''data:'')===0?rawBase64:(''data:application/pdf;base64,''+rawBase64);'),
                    updatedat = now()
                WHERE name = 'clicksign-normalizar-envio';
                """);
        }

        public override void Down()
        {
            // Ajuste de texto de funcao JS; nao ha estado anterior a restaurar.
        }
    }
}
