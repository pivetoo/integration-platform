using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations
{
    // A validacao do InputSchema no enqueue/execute passou a tratar campo sem '?' como obrigatorio. Dois
    // campos que o Mainstay deixa nulos por desenho estavam declarados como obrigatorios nos seeds e o
    // pipeline ja tolera o valor ausente: 'method' de cobranca.criar (o Mainstay nao envia, o pipeline do
    // Sicredi usa vazio) e 'creatorDocument' de pagamento.pix (Creator.Document e nullable e nenhum
    // pipeline de pagamento le o campo). Sem isto a emissao de cobranca e o repasse falhariam com 400.
    //
    // SeedContract so INSERE (WHERE NOT EXISTS); atualizar contrato existente exige UPDATE explicito.
    // O REPLACE e idempotente: depois de aplicado o texto original deixa de existir.
    [Migration(202609280002)]
    public sealed class Migration_202609280002_RelaxInputSchemaNullableFields : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE servicecontract
                SET inputschema = REPLACE(inputschema, '"method": "boleto | pix"', '"method": "boleto | pix?"')
                WHERE identifier = 'cobranca.criar';

                UPDATE servicecontract
                SET inputschema = REPLACE(inputschema, '"creatorDocument": "string"', '"creatorDocument": "string?"')
                WHERE identifier = 'pagamento.pix';
                """);
        }

        public override void Down()
        {
            Execute.Sql("""
                UPDATE servicecontract
                SET inputschema = REPLACE(inputschema, '"method": "boleto | pix?"', '"method": "boleto | pix"')
                WHERE identifier = 'cobranca.criar';

                UPDATE servicecontract
                SET inputschema = REPLACE(inputschema, '"creatorDocument": "string?"', '"creatorDocument": "string"')
                WHERE identifier = 'pagamento.pix';
                """);
        }
    }
}
