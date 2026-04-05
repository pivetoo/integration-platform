import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { pipelineEtapaService } from '../../services/pipelineEtapaService';
import { chamadaApiService } from '../../services/chamadaApiService';
import { funcaoJavaScriptService } from '../../services/funcaoJavaScriptService';
import { scriptBancoDadosService } from '../../services/scriptBancoDadosService';
import { TipoEtapa, AcaoErro } from '../../types/pipeline';
import type { PipelineEtapa, CreatePipelineEtapaRequest } from '../../types/pipeline';
import type { ChamadaApi } from '../../types/chamadaApi';
import type { FuncaoJavaScript } from '../../types/funcaoJavaScript';
import type { ScriptBancoDados } from '../../types/scriptBancoDados';

interface PipelineEtapaFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  etapa: PipelineEtapa | null;
  pipelineId: number;
  integracaoId: number;
  nextOrdem: number;
  onSuccess: () => void;
}

export default function PipelineEtapaFormModal({ open, onOpenChange, etapa, pipelineId, integracaoId, nextOrdem, onSuccess }: PipelineEtapaFormModalProps) {
  const isEditing = !!etapa;
  const { t } = useI18n();
  const [formData, setFormData] = useState<CreatePipelineEtapaRequest>({
    pipelineId,
    ordem: nextOrdem,
    nome: '',
    tipo: TipoEtapa.RequisicaoHttp,
    chamadaApiId: undefined,
    funcaoJavaScriptId: undefined,
    scriptBancoDadosId: undefined,
    aoErro: AcaoErro.Parar,
    ativo: true,
    ignorarNoRetorno: false,
  });
  const [chamadas, setChamadas] = useState<ChamadaApi[]>([]);
  const [funcoes, setFuncoes] = useState<FuncaoJavaScript[]>([]);
  const [scripts, setScripts] = useState<ScriptBancoDados[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchChamadas, loading: loadingChamadas } = useApi<ChamadaApi[]>({
    showErrorMessage: true,
  });

  const { execute: fetchFuncoes, loading: loadingFuncoes } = useApi<FuncaoJavaScript[]>({
    showErrorMessage: true,
  });

  const { execute: fetchScripts, loading: loadingScripts } = useApi<ScriptBancoDados[]>({
    showErrorMessage: true,
  });

  const loadingReferencias = loadingChamadas || loadingFuncoes || loadingScripts;

  useEffect(() => {
    if (open) {
      loadChamadas();
      loadFuncoes();
      loadScripts();
    }
  }, [open, integracaoId]);

  useEffect(() => {
    if (etapa) {
      setFormData({
        pipelineId,
        ordem: etapa.ordem,
        nome: etapa.nome,
        tipo: etapa.tipo,
        chamadaApiId: etapa.chamadaApi?.id,
        funcaoJavaScriptId: etapa.funcaoJavaScript?.id,
        scriptBancoDadosId: etapa.scriptBancoDados?.id,
        aoErro: etapa.aoErro,
        ativo: etapa.ativo,
        ignorarNoRetorno: !!etapa.ignorarNoRetorno,
      });
    } else {
      setFormData({
        pipelineId,
        ordem: nextOrdem,
        nome: '',
        tipo: TipoEtapa.RequisicaoHttp,
        chamadaApiId: undefined,
        funcaoJavaScriptId: undefined,
        scriptBancoDadosId: undefined,
        aoErro: AcaoErro.Parar,
        ativo: true,
        ignorarNoRetorno: false,
      });
    }
  }, [etapa, pipelineId, nextOrdem]);

  const loadChamadas = async () => {
    const result = await fetchChamadas(async () => {
      const res = await chamadaApiService.getAll({ pageSize: 1000 });
      return res.data;
    });
    if (result) {
      setChamadas(result);
    }
  };

  const loadFuncoes = async () => {
    const result = await fetchFuncoes(async () => {
      const res = await funcaoJavaScriptService.getAll({ pageSize: 1000 });
      return res.data;
    });
    if (result) {
      setFuncoes(result);
    }
  };

  const loadScripts = async () => {
    const result = await fetchScripts(async () => {
      const res = await scriptBancoDadosService.getAll({ pageSize: 1000 });
      return res.data;
    });
    if (result) {
      setScripts(result);
    }
  };

  const handleChange = (field: keyof CreatePipelineEtapaRequest, value: string | number | boolean | undefined) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => pipelineEtapaService.update(etapa.id, formData));
      } else {
        await execute(() => pipelineEtapaService.create(formData));
      }
      onSuccess();
    } catch {
      // Error handled by useApi
    }
  };

  const tipoOptions = [
    { value: TipoEtapa.RequisicaoHttp.toString(), label: t('pipeline.step.type.httpRequest') },
    { value: TipoEtapa.FuncaoJavaScript.toString(), label: t('pipeline.step.type.javaScriptFunction') },
    { value: TipoEtapa.ExecutarScript.toString(), label: t('pipeline.step.type.executeSqlScript') },
  ];
  const acaoErroOptions = [
    { value: AcaoErro.Parar.toString(), label: t('pipeline.step.errorAction.stop') },
    { value: AcaoErro.Continuar.toString(), label: t('pipeline.step.errorAction.continue') },
  ];

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('pipeline.step.form.editTitle') : t('pipeline.step.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2 col-span-2">
              <label htmlFor="nome" className="text-sm font-medium">{t('common.column.name')}</label>
              <Input
                id="nome"
                value={formData.nome}
                onChange={(e) => handleChange('nome', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.column.type')}</label>
                <Select
                  value={formData.tipo.toString()}
                  onValueChange={(value) => handleChange('tipo', parseInt(value))}
                  disabled={loadingReferencias}
                >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {tipoOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('pipeline.detail.onErrorLabel')}</label>
                <Select
                  value={formData.aoErro.toString()}
                  onValueChange={(value) => handleChange('aoErro', parseInt(value))}
                  disabled={loadingReferencias}
                >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {acaoErroOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {formData.tipo === TipoEtapa.RequisicaoHttp && (
              <div className="space-y-2 col-span-2">
                <label className="text-sm font-medium">{t('pipeline.detail.apiCall')}</label>
                <Select
                  value={formData.chamadaApiId?.toString() || '_none'}
                  onValueChange={(value) => handleChange('chamadaApiId', value === '_none' ? undefined : parseInt(value))}
                  disabled={loadingReferencias}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={loadingReferencias ? t('pipeline.step.form.loadingApiCalls') : t('pipeline.step.form.apiCallPlaceholder')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="_none">{t('common.option.none')}</SelectItem>
                    {chamadas.map((chamada) => (
                      <SelectItem key={chamada.id} value={chamada.id.toString()}>
                        {chamada.nome}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {formData.tipo === TipoEtapa.FuncaoJavaScript && (
              <div className="space-y-2 col-span-2">
                <label className="text-sm font-medium">{t('pipeline.detail.javaScriptFunction')}</label>
                <Select
                  value={formData.funcaoJavaScriptId?.toString() || '_none'}
                  onValueChange={(value) => handleChange('funcaoJavaScriptId', value === '_none' ? undefined : parseInt(value))}
                  disabled={loadingReferencias}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={loadingReferencias ? t('pipeline.step.form.loadingJavaScriptFunctions') : t('pipeline.step.form.javaScriptFunctionPlaceholder')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="_none">{t('common.option.none')}</SelectItem>
                    {funcoes.map((funcao) => (
                      <SelectItem key={funcao.id} value={funcao.id.toString()}>
                        {funcao.nome}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {formData.tipo === TipoEtapa.ExecutarScript && (
              <div className="space-y-2 col-span-2">
                <label className="text-sm font-medium">{t('pipeline.step.form.databaseScriptLabel')}</label>
                <Select
                  value={formData.scriptBancoDadosId?.toString() || '_none'}
                  onValueChange={(value) => handleChange('scriptBancoDadosId', value === '_none' ? undefined : parseInt(value))}
                  disabled={loadingReferencias}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={loadingReferencias ? t('pipeline.step.form.loadingDatabaseScripts') : t('pipeline.step.form.databaseScriptPlaceholder')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="_none">{t('common.option.none')}</SelectItem>
                    {scripts.map((script) => (
                      <SelectItem key={script.id} value={script.id.toString()}>
                        {script.nome}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-6">
            <div className="flex items-center space-x-2">
              <Checkbox
                id="ativo"
                checked={formData.ativo}
                onCheckedChange={(checked) => handleChange('ativo', !!checked)}
              />
              <label htmlFor="ativo" className="text-sm font-medium">{t('common.column.active')}</label>
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox
                id="ignorarNoRetorno"
                checked={!!formData.ignorarNoRetorno}
                onCheckedChange={(checked) => handleChange('ignorarNoRetorno', !!checked)}
              />
              <label htmlFor="ignorarNoRetorno" className="text-sm font-medium">{t('pipeline.step.form.ignoreOnApiReturn')}</label>
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              {t('common.action.cancel')}
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? t('common.action.saving') : t('common.action.save')}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  );
}
