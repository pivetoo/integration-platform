using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Converge as CATEGORIAS para o conjunto canonico que o AgencyCampaign assume
    // (IntegrationCategoryIdentifier). Idempotente: cria o que falta, no-op onde existe.
    // Os service contracts sao semeados numa migration propria de seed do catalogo.
    [Migration(202606070001)]
    public sealed class Migration_202606070001_SeedAgencyCampaignCanonicalCatalog : Migration
    {
        public override void Up()
        {
            SeedCategory("email", "Email", "Provedores de envio de e-mail transacional.");
            SeedCategory("whatsapp", "WhatsApp", "Provedores de mensageria WhatsApp.");
            SeedCategory("digital-signature", "Assinatura digital", "Provedores de coleta de assinaturas digitais.");
            SeedCategory("contas-a-receber", "Contas a receber", "Provedores de cobrança (boleto, PIX, cartão).");
            SeedCategory("contas-a-pagar", "Contas a pagar", "Provedores de repasse e pagamento (PIX, TED).");

            // Remove 'messaging' orfa (so se nenhuma integration nem servicecontract a referenciar)
            Execute.Sql(@"
                DELETE FROM integrationcategory
                WHERE identifier = 'messaging'
                  AND NOT EXISTS (SELECT 1 FROM integration WHERE integrationcategoryid = integrationcategory.id)
                  AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE integrationcategoryid = integrationcategory.id);
            ");
        }

        public override void Down()
        {
            // Convergencia de dados; Down nao reverte para nao reintroduzir o estado inconsistente anterior.
        }

        private void SeedCategory(string identifier, string name, string description)
        {
            string escapedName = name.Replace("'", "''");
            string escapedDescription = description.Replace("'", "''");
            Execute.Sql($@"
                INSERT INTO integrationcategory (identifier, name, description, isactive, issystem, createdat, updatedat)
                SELECT '{identifier}', '{escapedName}', '{escapedDescription}', true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = '{identifier}');
            ");
        }
    }
}
