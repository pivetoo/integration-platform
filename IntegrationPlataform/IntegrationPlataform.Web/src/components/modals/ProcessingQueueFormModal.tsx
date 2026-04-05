import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { processingQueueService } from '../../services/processingQueueService';
import { connectorService } from '../../services/connectorService';
import { pipelineService } from '../../services/pipelineService';
import type { CreateProcessingQueueItemRequest } from '../../types/processingQueue';
import type { Conector } from '../../types/connector';
import type { Pipeline } from '../../types/pipeline';

interface ProcessingQueueFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

const initialFormData: CreateProcessingQueueItemRequest = {
  connectorId: 0,
  pipelineId: 0,
  priority: 0,
  payload: '',
  scheduledAt: '',
};

export default function ProcessingQueueFormModal({ open, onOpenChange, onSuccess }: ProcessingQueueFormModalProps) {
  const [formData, setFormData] = useState<CreateProcessingQueueItemRequest>(initialFormData);
  const [conectores, setConectores] = useState<Conector[]>([]);
  const [pipelines, setPipelines] = useState<Pipeline[]>([]);
  const { t } = useI18n();

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchConectores } = useApi<Conector[]>({
    showErrorMessage: true,
  });

  const { execute: fetchPipelines } = useApi<Pipeline[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadConectores();
      loadPipelines();
      setFormData(initialFormData);
    }
  }, [open]);

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

  const handleChange = (field: keyof CreateProcessingQueueItemRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const data = {
      ...formData,
      scheduledAt: formData.scheduledAt || undefined,
      payload: formData.payload || undefined,
    };
    const result = await execute(() => processingQueueService.create(data));

    if (result !== null) {
      onSuccess();
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader>
          <ModalTitle>{t('queue.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.column.connector')}</label>
              <Select
                value={formData.connectorId ? formData.connectorId.toString() : ''}
                onValueChange={(value) => handleChange('connectorId', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('queue.form.connectorPlaceholder')} />
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
                onValueChange={(value) => handleChange('pipelineId', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('queue.form.pipelinePlaceholder')} />
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
              <label htmlFor="priority" className="text-sm font-medium">{t('common.column.priority')}</label>
              <Input
                id="priority"
                type="number"
                value={formData.priority}
                onChange={(e) => handleChange('priority', parseInt(e.target.value) || 0)}
                min={0}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="scheduledAt" className="text-sm font-medium">{t('queue.form.scheduledAt')}</label>
              <Input
                id="scheduledAt"
                type="datetime-local"
                value={formData.scheduledAt || ''}
                onChange={(e) => handleChange('scheduledAt', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="payload" className="text-sm font-medium">{t('queue.form.payloadLabel')}</label>
              <textarea
                id="payload"
                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={formData.payload || ''}
                onChange={(e) => handleChange('payload', e.target.value)}
                rows={3}
              />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              {t('common.action.cancel')}
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? t('queue.form.submitting') : t('queue.form.submit')}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  );
}
