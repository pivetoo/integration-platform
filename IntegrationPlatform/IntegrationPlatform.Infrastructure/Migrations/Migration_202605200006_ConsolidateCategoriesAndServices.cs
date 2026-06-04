using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200006)]
    public sealed class Migration_202605200006_ConsolidateCategoriesAndServices : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                UPDATE servicecontract
                SET identifier = 'whatsapp.send',
                    name = 'Envio de mensagem WhatsApp',
                    description = 'Envia mensagem via WhatsApp.',
                    integrationcategoryid = (SELECT id FROM integrationcategory WHERE identifier = 'whatsapp')
                WHERE identifier = 'messaging.send'
                  AND EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = 'whatsapp');
            ");

            Execute.Sql(@"
                INSERT INTO servicecontract (identifier, name, description, integrationcategoryid, inputschema, outputschema, hascallback, callbackschema, isactive, issystem, createdat, updatedat)
                SELECT 'payment.charge.create', 'Criar cobranca', 'Emite cobranca para cliente (boleto/PIX/cartao).', id, NULL, NULL, true, NULL, true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                FROM integrationcategory WHERE identifier = 'contas-a-receber'
                  AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE identifier = 'payment.charge.create');
            ");

            Execute.Sql(@"
                INSERT INTO servicecontract (identifier, name, description, integrationcategoryid, inputschema, outputschema, hascallback, callbackschema, isactive, issystem, createdat, updatedat)
                SELECT 'payment.transfer.create', 'Criar repasse', 'Repassa valor (PIX/TED) para creator ou fornecedor.', id, NULL, NULL, true, NULL, true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                FROM integrationcategory WHERE identifier = 'contas-a-pagar'
                  AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE identifier = 'payment.transfer.create');
            ");

            Execute.Sql(@"
                DELETE FROM integrationservicecontract
                WHERE servicecontractid IN (SELECT id FROM servicecontract WHERE identifier = 'banking.account.sync');
            ");
            Execute.Sql("DELETE FROM servicecontract WHERE identifier = 'banking.account.sync';");

            Execute.Sql(@"
                INSERT INTO integrationservicecontract (integrationid, servicecontractid, isactive, createdat, updatedat)
                SELECT i.id, sc.id, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                FROM integration i
                INNER JOIN servicecontract sc ON sc.integrationcategoryid = i.integrationcategoryid
                WHERE NOT EXISTS (SELECT 1 FROM integrationservicecontract isc WHERE isc.integrationid = i.id AND isc.servicecontractid = sc.id);
            ");

            Execute.Sql(@"
                UPDATE pipeline SET servicecontractid = (SELECT id FROM servicecontract WHERE identifier = 'whatsapp.send'), isdefault = true
                WHERE servicecontractid IS NULL AND identifier LIKE '%-send-message';
            ");
            Execute.Sql(@"
                UPDATE pipeline SET servicecontractid = (SELECT id FROM servicecontract WHERE identifier = 'payment.transfer.create'), isdefault = true
                WHERE servicecontractid IS NULL AND identifier LIKE '%-send-pix';
            ");
            Execute.Sql(@"
                UPDATE pipeline SET servicecontractid = (SELECT id FROM servicecontract WHERE identifier = 'payment.charge.create'), isdefault = true
                WHERE servicecontractid IS NULL AND (identifier = 'gerarCobranca' OR identifier LIKE '%-create-charge' OR identifier LIKE '%-create-cobranca');
            ");

            // So remove a categoria se NENHUMA integration E NENHUM servicecontract a referenciam.
            // Num banco limpo, 'messaging.send' segue apontando para 'messaging' (a consolidacao para
            // 'whatsapp' nao roda porque a categoria 'whatsapp' nao existe), entao sem o guard de
            // servicecontract o DELETE violaria a FK servicecontract_integrationcategoryid_fkey.
            Execute.Sql(@"
                DELETE FROM integrationcategory
                WHERE identifier IN ('banking', 'messaging')
                  AND NOT EXISTS (SELECT 1 FROM integration WHERE integrationcategoryid = integrationcategory.id)
                  AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE integrationcategoryid = integrationcategory.id);
            ");
        }

        public override void Down()
        {
        }
    }
}
