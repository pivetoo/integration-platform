import { useEffect, useState } from 'react';
import { Modal, ModalBody, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { pipelineStepService } from '../../services/pipelineStepService';
import { apiCallService } from '../../services/apiCallService';
import { javaScriptFunctionService } from '../../services/javaScriptFunctionService';
import { databaseScriptService } from '../../services/databaseScriptService';
import { PipelineStepType, ErrorAction } from '../../types/pipeline';
import type { PipelineStep, CreatePipelineStepRequest } from '../../types/pipeline';
import type { ApiCall } from '../../types/apiCall';
import type { JavaScriptFunction } from '../../types/javaScriptFunction';
import type { DatabaseScript } from '../../types/databaseScript';

interface PipelineStepFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  step: PipelineStep | null;
  pipelineId: number;
  integrationId: number;
  nextOrder: number;
  onSuccess: () => void;
}

export default function PipelineStepFormModal({ open, onOpenChange, step, pipelineId, integrationId, nextOrder, onSuccess }: PipelineStepFormModalProps) {
  const isEditing = !!step;
  const { t } = useI18n();
  const createInitialFormData = (): CreatePipelineStepRequest => ({
    pipelineId,
    order: nextOrder,
    name: '',
    type: PipelineStepType.HttpRequest,
    apiCallId: undefined,
    javaScriptFunctionId: undefined,
    databaseScriptId: undefined,
    errorAction: ErrorAction.Stop,
    isActive: true,
    ignoreOnResponse: false,
    runOnError: false,
    runCondition: '',
  });
  const [formData, setFormData] = useState<CreatePipelineStepRequest>(createInitialFormData);
  const [chamadas, setChamadas] = useState<ApiCall[]>([]);
  const [funcoes, setFuncoes] = useState<JavaScriptFunction[]>([]);
  const [scripts, setScripts] = useState<DatabaseScript[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchChamadas, loading: loadingChamadas } = useApi<ApiCall[]>({
    showErrorMessage: true,
  });

  const { execute: fetchFuncoes, loading: loadingFuncoes } = useApi<JavaScriptFunction[]>({
    showErrorMessage: true,
  });

  const { execute: fetchScripts, loading: loadingScripts } = useApi<DatabaseScript[]>({
    showErrorMessage: true,
  });

  const loadingReferencias = loadingChamadas || loadingFuncoes || loadingScripts;

  useEffect(() => {
    if (open) {
      loadChamadas();
      loadFuncoes();
      loadScripts();
    }
  }, [open, integrationId]);

  useEffect(() => {
    if (step) {
      setFormData({
        pipelineId,
        order: step.order,
        name: step.name,
        type: step.type,
        apiCallId: step.apiCall?.id,
        javaScriptFunctionId: step.javaScriptFunction?.id,
        databaseScriptId: step.databaseScript?.id,
        errorAction: step.errorAction,
        isActive: step.isActive,
        ignoreOnResponse: !!step.ignoreOnResponse,
        runOnError: !!step.runOnError,
        runCondition: step.runCondition ?? '',
      });
    } else {
      setFormData(createInitialFormData());
    }
  }, [step, pipelineId, nextOrder]);

  const loadChamadas = async () => {
    const result = await fetchChamadas(async () => {
      const res = await apiCallService.getAll({ pageSize: 1000 });
      return res.data;
    });
    if (result) {
      setChamadas(result);
    }
  };

  const loadFuncoes = async () => {
    const result = await fetchFuncoes(async () => {
      const res = await javaScriptFunctionService.getAll({ pageSize: 1000 });
      return res.data;
    });
    if (result) {
      setFuncoes(result);
    }
  };

  const loadScripts = async () => {
    const result = await fetchScripts(async () => {
      const res = await databaseScriptService.getAll({ pageSize: 1000 });
      return res.data;
    });
    if (result) {
      setScripts(result);
    }
  };

  const handleChange = (field: keyof CreatePipelineStepRequest, value: string | number | boolean | undefined) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const result = await execute(() =>
      isEditing
        ? pipelineStepService.update(step.id, formData)
        : pipelineStepService.create(formData)
    );

    if (result !== null) {
      onSuccess();
    }
  };

  const tipoOptions = [
    { value: PipelineStepType.HttpRequest.toString(), label: t('pipeline.step.type.httpRequest') },
    { value: PipelineStepType.JavaScriptFunction.toString(), label: t('pipeline.step.type.javaScriptFunction') },
    { value: PipelineStepType.ExecuteScript.toString(), label: t('pipeline.step.type.executeSqlScript') },
    { value: PipelineStepType.SmtpSend.toString(), label: t('pipeline.step.type.smtpSend') },
  ];
  const acaoErroOptions = [
    { value: ErrorAction.Stop.toString(), label: t('pipeline.step.errorAction.stop') },
    { value: ErrorAction.Continue.toString(), label: t('pipeline.step.errorAction.continue') },
  ];

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('pipeline.step.form.editTitle') : t('pipeline.step.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-3 sm:gap-4">
          <ModalBody className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2 col-span-2">
                <label htmlFor="name" className="text-sm font-medium">{t('common.column.name')}</label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => handleChange('name', e.target.value)}
                  required
                />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">{t('common.column.type')}</label>
                <Select
                  value={formData.type.toString()}
                  onValueChange={(value) => handleChange('type', parseInt(value))}
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
                  value={formData.errorAction.toString()}
                  onValueChange={(value) => handleChange('errorAction', parseInt(value))}
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

              {formData.type === PipelineStepType.HttpRequest && (
                <div className="space-y-2 col-span-2">
                  <label className="text-sm font-medium">{t('pipeline.detail.apiCall')}</label>
                  <Select
                    value={formData.apiCallId?.toString() || '_none'}
                    onValueChange={(value) => handleChange('apiCallId', value === '_none' ? undefined : parseInt(value))}
                    disabled={loadingReferencias}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder={loadingReferencias ? t('pipeline.step.form.loadingApiCalls') : t('pipeline.step.form.apiCallPlaceholder')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="_none">{t('common.option.none')}</SelectItem>
                      {chamadas.map((chamada) => (
                        <SelectItem key={chamada.id} value={chamada.id.toString()}>
                          {chamada.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              {formData.type === PipelineStepType.JavaScriptFunction && (
                <div className="space-y-2 col-span-2">
                  <label className="text-sm font-medium">{t('pipeline.detail.javaScriptFunction')}</label>
                  <Select
                    value={formData.javaScriptFunctionId?.toString() || '_none'}
                    onValueChange={(value) => handleChange('javaScriptFunctionId', value === '_none' ? undefined : parseInt(value))}
                    disabled={loadingReferencias}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder={loadingReferencias ? t('pipeline.step.form.loadingJavaScriptFunctions') : t('pipeline.step.form.javaScriptFunctionPlaceholder')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="_none">{t('common.option.none')}</SelectItem>
                      {funcoes.map((funcao) => (
                        <SelectItem key={funcao.id} value={funcao.id.toString()}>
                          {funcao.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              {formData.type === PipelineStepType.ExecuteScript && (
                <div className="space-y-2 col-span-2">
                  <label className="text-sm font-medium">{t('pipeline.step.form.databaseScriptLabel')}</label>
                  <Select
                    value={formData.databaseScriptId?.toString() || '_none'}
                    onValueChange={(value) => handleChange('databaseScriptId', value === '_none' ? undefined : parseInt(value))}
                    disabled={loadingReferencias}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder={loadingReferencias ? t('pipeline.step.form.loadingDatabaseScripts') : t('pipeline.step.form.databaseScriptPlaceholder')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="_none">{t('common.option.none')}</SelectItem>
                      {scripts.map((script) => (
                        <SelectItem key={script.id} value={script.id.toString()}>
                          {script.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('pipeline.step.form.runCondition')}</label>
              <Input
                value={formData.runCondition ?? ''}
                onChange={(event) => handleChange('runCondition', event.target.value)}
                placeholder="!variables.existingCustomerId"
                maxLength={1000}
              />
              <p className="text-xs text-muted-foreground">{t('pipeline.step.form.runCondition.help')}</p>
            </div>

            <div className="flex flex-wrap items-center gap-6 pt-1">
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isActive"
                  checked={formData.isActive}
                  onCheckedChange={(checked) => handleChange('isActive', !!checked)}
                />
                <label htmlFor="isActive" className="text-sm font-medium cursor-pointer">{t('common.column.active')}</label>
              </div>

              <div className="flex items-center space-x-2">
                <Checkbox
                  id="ignoreOnResponse"
                  checked={!!formData.ignoreOnResponse}
                  onCheckedChange={(checked) => handleChange('ignoreOnResponse', !!checked)}
                />
                <label htmlFor="ignoreOnResponse" className="text-sm font-medium cursor-pointer">{t('pipeline.step.form.ignoreOnApiReturn')}</label>
              </div>

              <div className="flex items-center space-x-2">
                <Checkbox
                  id="runOnError"
                  checked={!!formData.runOnError}
                  onCheckedChange={(checked) => handleChange('runOnError', !!checked)}
                />
                <label htmlFor="runOnError" className="text-sm font-medium cursor-pointer">{t('pipeline.step.form.runOnError')}</label>
              </div>
            </div>
          </ModalBody>

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
