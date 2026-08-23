using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Conta padrao por categoria: substitui o vinculo intent->conector dos consumidores (AgencyCampaign
    // removeu a tela "Acoes"). A primeira conta ativa de cada categoria vira a padrao.
    [Migration(202608230001)]
    public sealed class Migration_202608230001_AddConnectorIsDefault : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("connector").Column("isdefault").Exists())
            {
                Alter.Table("connector").AddColumn("isdefault").AsBoolean().NotNullable().WithDefaultValue(false);
            }

            // Conversao de dado: elege a conta ativa mais antiga de cada categoria que ainda nao tem padrao.
            Execute.Sql(@"
                UPDATE connector SET isdefault = true
                WHERE id IN (
                    SELECT DISTINCT ON (i.integrationcategoryid) c.id
                    FROM connector c
                    JOIN integration i ON i.id = c.integrationid
                    WHERE c.isactive
                      AND NOT EXISTS (
                          SELECT 1 FROM connector c2
                          JOIN integration i2 ON i2.id = c2.integrationid
                          WHERE c2.isdefault AND i2.integrationcategoryid = i.integrationcategoryid)
                    ORDER BY i.integrationcategoryid, c.id
                );");
        }

        public override void Down()
        {
            if (Schema.Table("connector").Column("isdefault").Exists())
            {
                Delete.Column("isdefault").FromTable("connector");
            }
        }
    }
}
