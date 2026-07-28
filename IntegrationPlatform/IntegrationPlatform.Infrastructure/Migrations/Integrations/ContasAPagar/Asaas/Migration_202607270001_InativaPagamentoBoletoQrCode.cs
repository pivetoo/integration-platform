using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAPagar.Asaas
{
    // Inativa os contratos pagamento.boleto e pagamento.qrcode e seus pipelines Asaas.
    //
    // Motivo: os dois foram construidos completos no IntegrationPlatform, mas NAO tem consumidor do lado
    // do AgencyCampaign — a tela "Custos e pagamentos" nao integra com provedor nenhum (todo custo entra
    // por baixa manual). Ficar ativos no catalogo faz parecer recurso entregue, e a escolha do conector
    // por contrato passa a oferecer opcoes que nenhuma tela sabe disparar.
    //
    // Decisao (INT-021, 2026-07-26): inativar em vez de apagar. O codigo fica preservado e a reativacao e
    // uma migration de uma linha, quando pagar um custo pela linha digitavel / QR virar funcionalidade de
    // verdade (o caso real e midia paga e freela). Coerente com o escopo fechado do Contas a Pagar: nao
    // virar AP/ERP, nao criar entidade Fornecedor.
    //
    // pagamento.pix NAO e tocado: e o contrato do repasse ao creator, que esta em uso.
    [Migration(202607270001)]
    public sealed class Migration_202607270001_InativaPagamentoBoletoQrCode : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE pipeline SET isactive = false, updatedat = now()
                WHERE identifier IN ('asaas-pagamento-boleto', 'asaas-pagamento-qrcode');

                UPDATE servicecontract SET isactive = false, updatedat = now()
                WHERE identifier IN ('pagamento.boleto', 'pagamento.qrcode');
                """);
        }

        public override void Down()
        {
            Execute.Sql("""
                UPDATE pipeline SET isactive = true, updatedat = now()
                WHERE identifier IN ('asaas-pagamento-boleto', 'asaas-pagamento-qrcode');

                UPDATE servicecontract SET isactive = true, updatedat = now()
                WHERE identifier IN ('pagamento.boleto', 'pagamento.qrcode');
                """);
        }
    }
}
