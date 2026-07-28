using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Completa o mapeamento da emissao: campos que o AgencyCampaign ja enviava (ou passou a enviar) e o
    // pipeline descartava.
    //
    // - description: o boleto chegava a marca sem dizer a que se referia;
    // - fine / discount: multa e desconto nunca chegavam ao Asaas (so juros era mapeado), entao o
    //   inadimplente pagava o valor de face na data que quisesse;
    // - email / telefone do pagador: sem eles o Asaas nao consegue enviar o boleto nem os lembretes;
    // - complemento e bairro (province): bairro chegava no payload e era ignorado;
    // - endereco condicional: postalCode/address/addressNumber iam SEMPRE, mesmo vazios, o que expunha a
    //   emissao a 400 de validacao do provedor para marca sem endereco completo;
    // - invoiceUrl / bankSlipUrl: o Asaas devolve os dois na criacao e nenhum era capturado, entao
    //   ChargeUrl e ChargeBankSlipUrl ficavam sempre nulos no Mainstay.
    //
    // Aqui os artefatos sao ATUALIZADOS no lugar (UPDATE), nao apagados e re-semeados: pipelinestep
    // referencia apicall/javascriptfunction por FK sem cascade, e estes ja estao ligados aos steps do
    // 202607260002 — apagar violaria a FK.
    [Migration(202607260007)]
    public sealed class Migration_202607260007_AsaasCobrancaCamposCompletos : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE javascriptfunction SET updatedat = now(), description =
                    'Monta o body do POST /customers (nome, CPF/CNPJ, contato e endereço apenas quando preenchidos) e deriva dueDate + extrasFrag, o fragmento condicional da cobrança: interest (juros ao mês), fine (multa fixa), discount (desconto até a data limite) e description.',
                    code = $js$
                function s(v){return v==null?'':(''+v);}
                function d(v){return s(v).replace(/\D/g,'');}
                var doc=d(payload.payerDocument);
                var due=s(payload.dueAt).substring(0,10);
                var cep=d(payload.payerCep);
                var fone=d(payload.payerPhone);
                var customerBody={name:s(payload.payerName),cpfCnpj:doc,externalReference:s(payload.financialEntryId)};
                if(s(payload.payerEmail)){customerBody.email=s(payload.payerEmail);}
                if(fone){customerBody.mobilePhone=fone;}
                if(cep){customerBody.postalCode=cep;}
                if(s(payload.payerStreet)){customerBody.address=s(payload.payerStreet);}
                if(s(payload.payerNumber)){customerBody.addressNumber=s(payload.payerNumber);}
                if(s(payload.payerComplement)){customerBody.complement=s(payload.payerComplement);}
                if(s(payload.payerNeighborhood)){customerBody.province=s(payload.payerNeighborhood);}
                var extras='';
                var juros=Number(payload.interestMonthlyPercent)||0;
                if(juros>0){extras+=',"interest":{"value":'+juros+'}';}
                var multa=Number(payload.fineValue)||0;
                if(multa>0){extras+=',"fine":{"value":'+multa+',"type":"FIXED"}';}
                var desconto=Number(payload.discountValue)||0;
                if(desconto>0){
                  var limite=0;
                  var ate=s(payload.discountUntil).substring(0,10);
                  if(ate&&due){
                    try{
                      var pa=ate.split('-');var pd=due.split('-');
                      var ma=Date.UTC(Number(pa[0]),Number(pa[1])-1,Number(pa[2]));
                      var md=Date.UTC(Number(pd[0]),Number(pd[1])-1,Number(pd[2]));
                      limite=Math.round((md-ma)/86400000);
                      if(!(limite>0)){limite=0;}
                    }catch(e){limite=0;}
                  }
                  extras+=',"discount":{"value":'+desconto+',"dueDateLimitDays":'+limite+',"type":"FIXED"}';
                }
                var descricao=s(payload.description).substring(0,500);
                if(descricao){extras+=',"description":'+JSON.stringify(descricao);}
                result.value={asaasCustomerBody:JSON.stringify(customerBody),asaasCpfCnpj:doc,dueDate:due,extrasFrag:extras};
                $js$
                WHERE name = 'asaas-normalizar-payload';
                """);

            Execute.Sql("""
                UPDATE javascriptfunction SET updatedat = now(), description =
                    'Consolida a emissão Asaas: chargeId, digitableLine/barCode/nossoNumero (identificationField), pixCopyPaste (payload do pixQrCode) e os links hospedados invoiceUrl/bankSlipUrl. Asaas não expõe txid.',
                    code = $js$
                function s(v){return v==null?'':(''+v);}
                result.value={
                  chargeId:s(variables.id),
                  digitableLine:s(variables.identificationField),
                  barCode:s(variables.barCode),
                  nossoNumeroAsaas:s(variables.nossoNumero),
                  pixCopyPaste:s(variables.payload),
                  txIdAsaas:'',
                  chargeUrlAsaas:s(variables.invoiceUrl),
                  bankSlipUrlAsaas:s(variables.bankSlipUrl)
                };
                $js$
                WHERE name = 'asaas-normalizar-resposta';
                """);

            Execute.Sql("""
                UPDATE apicall SET updatedat = now(), bodytemplate =
                    '{"customer":"{{customerId}}","billingType":"BOLETO","value":{{amount}},"dueDate":"{{dueDate}}","externalReference":"{{financialEntryId}}"{{extrasFrag}}}'
                WHERE name = 'Asaas - Criar cobrança';
                """);

            Execute.Sql("""
                UPDATE apicall SET updatedat = now(), bodytemplate =
                    '{"provider":"asaas","eventType":"issued","financialEntryId":{{financialEntryId}},"chargeId":{{chargeId | json}},"digitableLine":{{digitableLine | json}},"barCode":{{barCode | json}},"nossoNumero":{{nossoNumeroAsaas | json}},"pixCopyPaste":{{pixCopyPaste | json}},"txId":{{txIdAsaas | json}},"chargeUrl":{{chargeUrlAsaas | json}},"bankSlipUrl":{{bankSlipUrlAsaas | json}}}'
                WHERE name = 'Asaas - Callback issued';
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
