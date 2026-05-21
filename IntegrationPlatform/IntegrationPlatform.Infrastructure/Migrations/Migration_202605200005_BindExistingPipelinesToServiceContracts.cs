using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200005)]
    public sealed class Migration_202605200005_BindExistingPipelinesToServiceContracts : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                UPDATE pipeline
                SET servicecontractid = (SELECT id FROM servicecontract WHERE identifier = 'email.send'),
                    isdefault = true
                WHERE servicecontractid IS NULL
                  AND identifier LIKE '%-send-email'
                  AND integrationid IN (SELECT id FROM integration WHERE integrationcategoryid = (SELECT id FROM integrationcategory WHERE identifier = 'email'));
            ");

            Execute.Sql(@"
                UPDATE pipeline
                SET servicecontractid = (SELECT id FROM servicecontract WHERE identifier = 'signature.envelope.create'),
                    isdefault = true
                WHERE servicecontractid IS NULL
                  AND identifier LIKE '%-send-document'
                  AND integrationid IN (SELECT id FROM integration WHERE integrationcategoryid = (SELECT id FROM integrationcategory WHERE identifier = 'digital-signature'));
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                UPDATE pipeline
                SET servicecontractid = NULL, isdefault = false
                WHERE servicecontractid IN (
                    SELECT id FROM servicecontract WHERE identifier IN ('email.send', 'signature.envelope.create')
                );
            ");
        }
    }
}
