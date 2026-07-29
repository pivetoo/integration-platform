using FluentMigrator;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Buscar linha digitavel e QR PIX e ENRIQUECIMENTO: a cobranca ja existe no provedor quando esses
    // GETs rodam. Com ErrorAction.Stop, uma falha ali abortava o pipeline depois do efeito colateral.
    //
    // Evidencia (execucao 58): conta Asaas sem chave PIX cadastrada. O POST /payments deu 200, a linha
    // digitavel veio 200, e o GET /payments/{id}/pixQrCode devolveu 400 "Você não possui uma chave Pix
    // cadastrada". O pipeline parou ali, o step "Normalizar resposta" nunca rodou e a reentrega de erro
    // mandou so o chargeId — o recebivel ficou "Emitida" SEM linha digitavel, SEM PDF e SEM PIX, isto e,
    // com uma cobranca viva no provedor que o operador nao tinha como entregar ao pagador.
    //
    // No "alterar cobranca" era pior: o PUT ja tinha alterado a cobranca e a falha no enriquecimento
    // disparava o callback 'update_failed', dizendo ao Mainstay que a alteracao NAO aconteceu.
    //
    // Com ErrorAction.Continue esses dois steps viram aviso: o pipeline segue, normaliza o que conseguiu
    // obter e manda o callback verdadeiro (issued/updated) com os artefatos disponiveis. A execucao termina
    // Partial, entao o item da fila e concluido (nao ha reprocesso que gere cobranca duplicada) e o
    // operador ve exatamente qual enriquecimento faltou.
    //
    // Execute.Sql porque e alteracao de dado semeado (pipelinestep), nao DDL.
    [Migration(202607290001)]
    public sealed class Migration_202607290001_AsaasArtefatoNaoDerrubaEmissao : IntegrationSeedMigration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE pipelinestep ps
                SET erroraction = 2, updatedat = now()
                FROM pipeline p
                WHERE p.id = ps.pipelineid
                  AND p.identifier IN ('asaas-criar-cobranca', 'asaas-alterar-cobranca')
                  AND ps.name IN ('Obter linha digitável', 'Obter QR PIX');
                """);
        }

        public override void Down()
        {
            Execute.Sql("""
                UPDATE pipelinestep ps
                SET erroraction = 1, updatedat = now()
                FROM pipeline p
                WHERE p.id = ps.pipelineid
                  AND p.identifier IN ('asaas-criar-cobranca', 'asaas-alterar-cobranca')
                  AND ps.name IN ('Obter linha digitável', 'Obter QR PIX');
                """);
        }
    }
}
