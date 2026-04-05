import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n, toast } from 'archon-ui';
import { pipelineRoutineService } from '../../services/pipelineRoutineService';
import { connectorService } from '../../services/connectorService';
import { pipelineService } from '../../services/pipelineService';
import type { PipelineRoutine, CreatePipelineRoutineRequest, UpdatePipelineRoutineRequest } from '../../types/pipelineRoutine';
import type { Conector } from '../../types/connector';
import type { Pipeline } from '../../types/pipeline';

interface PipelineRoutineFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  rotina: PipelineRoutine | null;
  onSuccess: () => void;
}

const initialFormData: CreatePipelineRoutineRequest = {
  connectorId: 0,
  pipelineId: 0,
  isActive: true,
  intervalMinutes: 60,
  defaultPayload: '',
  nextExecution: '',
};

export default function PipelineRoutineFormModal({ open, onOpenChange, rotina, onSuccess }: PipelineRoutineFormModalProps) {
  const isEditing = !!rotina;
  const [formData, setFormData] = useState<CreatePipelineRoutineRequest>(initialFormData);
  const [conectores, setConectores] = useState<Conector[]>([]);
  const [pipelines, setPipelines] = useState<Pipeline[]>([]);
  const { t } = useI18n();

  const { execute, loading } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({
        title: t('common.toast.successTitle'),
        description: isEditing ? t('pipeline.routine.form.updated') : t('pipeline.routine.form.created'),
        variant: 'success',
      });
    },
  });

  const { execute: fetchConectores } = useApi<Conector[]>({ showErrorMessage: true });
  const { execute: fetchPipelines } = useApi<Pipeline[]>({ showErrorMessage: true });

  useEffect(() => {
    if (open) {
      loadConectores();
      loadPipelines();
    }
  }, [open]);

  useEffect(() => {
    if (rotina) {
      setFormData({
        connectorId: rotina.connector?.id || 0,
        pipelineId: rotina.pipeline?.id || 0,
        isActive: rotina.isActive,
        intervalMinutes: rotina.intervalMinutes,
        defaultPayload: rotina.defaultPayload || '',
        nextExecution: rotina.nextExecution ? rotina.nextExecution.slice(0, 16) : '',
      });
      return;
    }

    setFormData(initialFormData);
  }, [rotina]);

  const loadConectores = async () => {
    const result = await fetchConectores(() => connectorService.getActive());
    if (result) {
      setConectores(result);
    }
  };

  const loadPipelines = async () => {
    const result = await fetchPipelines(() => pipelineService.getActive());
    if (result) {
      setPipelines(result);
    }
  };

  const handleChange = (field: keyof CreatePipelineRoutineRequest, value: string | number | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const result = await execute(() => {
      if (isEditing && rotina) {
        const payload: UpdatePipelineRoutineRequest = {
          isActive: formData.isActive,
          intervalMinutes: formData.intervalMinutes,
          defaultPayload: formData.defaultPayload || undefined,
          nextExecution: formData.nextExecution || undefined,
        };

        return pipelineRoutineService.update(rotina.id, payload);
      }

      const payload: CreatePipelineRoutineRequest = {
        ...formData,
        defaultPayload: formData.defaultPayload || undefined,
        nextExecution: formData.nextExecution || undefined,
      };

      return pipelineRoutineService.create(payload);
    });

    if (result !== null) {
      onSuccess();
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('pipeline.routine.form.editTitle') : t('pipeline.routine.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.column.connector')}</label>
              <Select
                value={formData.connectorId ? formData.connectorId.toString() : ''}
                onValueChange={(value) => handleChange('connectorId', parseInt(value, 10))}
                disabled={isEditing}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('pipeline.routine.form.connectorPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {conectores.map((conector) => (
                    <SelectItem key={conector.id} value={conector.id.toString()}>
                      {conector.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.column.pipeline')}</label>
              <Select
                value={formData.pipelineId ? formData.pipelineId.toString() : ''}
                onValueChange={(value) => handleChange('pipelineId', parseInt(value, 10))}
                disabled={isEditing}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('pipeline.routine.form.pipelinePlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {pipelines.map((pipeline) => (
                    <SelectItem key={pipeline.id} value={pipeline.id.toString()}>
                      {pipeline.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label htmlFor="intervalMinutes" className="text-sm font-medium">{t('pipeline.routine.form.intervalMinutes')}</label>
              <Input
                id="intervalMinutes"
                type="number"
                min={1}
                value={formData.intervalMinutes}
                onChange={(e) => handleChange('intervalMinutes', parseInt(e.target.value, 10) || 1)}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="nextExecution" className="text-sm font-medium">{t('pipeline.routine.form.nextExecution')}</label>
              <Input
                id="nextExecution"
                type="datetime-local"
                value={formData.nextExecution || ''}
                onChange={(e) => handleChange('nextExecution', e.target.value)}
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="defaultPayload" className="text-sm font-medium">{t('pipeline.routine.form.defaultPayload')}</label>
              <textarea
                id="defaultPayload"
                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                rows={3}
                value={formData.defaultPayload || ''}
                onChange={(e) => handleChange('defaultPayload', e.target.value)}
              />
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox
              id="isActive"
              checked={formData.isActive}
              onCheckedChange={(checked) => handleChange('isActive', !!checked)}
            />
            <label htmlFor="isActive" className="text-sm font-medium">{t('common.column.active')}</label>
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
