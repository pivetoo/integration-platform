import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { pipelineService } from '../../services/pipelineService';
import { integracaoService } from '../../services/integracaoService';
import type { Pipeline, CreatePipelineRequest } from '../../types/pipeline';
import type { Integration } from '../../types/integracao';

interface PipelineFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  pipeline: Pipeline | null;
  onSuccess: () => void;
}

const initialFormData: CreatePipelineRequest = {
  integrationId: 0,
  identifier: '',
  name: '',
  description: '',
  isActive: true,
};

export default function PipelineFormModal({ open, onOpenChange, pipeline, onSuccess }: PipelineFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!pipeline;
  const [formData, setFormData] = useState<CreatePipelineRequest>(initialFormData);
  const [integrations, setIntegrations] = useState<Integration[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchIntegracoes } = useApi<Integration[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadIntegrations();
    }
  }, [open]);

  useEffect(() => {
    if (pipeline) {
      setFormData({
        integrationId: pipeline.integrationId || pipeline.integration?.id || 0,
        identifier: pipeline.identifier,
        name: pipeline.name,
        description: pipeline.description || '',
        isActive: pipeline.isActive,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [pipeline]);

  const loadIntegrations = async () => {
    const result = await fetchIntegracoes(() => integracaoService.getActive());
    if (result) {
      setIntegrations(result);
    }
  };

  const handleChange = (field: keyof CreatePipelineRequest, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => pipelineService.update(pipeline.id, formData));
      } else {
        await execute(() => pipelineService.create(formData));
      }
      onSuccess();
    } catch {
      // Error handled by useApi
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('pipeline.form.editTitle') : t('pipeline.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="identifier" className="text-sm font-medium">{t('common.column.identifier')}</label>
              <Input
                id="identifier"
                value={formData.identifier}
                onChange={(e) => handleChange('identifier', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="name" className="text-sm font-medium">{t('common.column.name')}</label>
              <Input
                id="name"
                value={formData.name}
                onChange={(e) => handleChange('name', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="description" className="text-sm font-medium">{t('common.column.description')}</label>
              <Input
                id="description"
                value={formData.description || ''}
                onChange={(e) => handleChange('description', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.column.integration')}</label>
              <Select
                value={formData.integrationId ? formData.integrationId.toString() : ''}
                onValueChange={(value) => handleChange('integrationId', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('pipeline.form.integrationPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {integrations.map((integration) => (
                    <SelectItem key={integration.id} value={integration.id.toString()}>
                      {integration.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
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
