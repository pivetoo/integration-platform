import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { conectorService } from '../../services/conectorService';
import { integracaoService } from '../../services/integracaoService';
import type { Conector, CreateConectorRequest } from '../../types/conector';
import type { Integration } from '../../types/integracao';

interface ConectorFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  connector: Conector | null;
  onSuccess: () => void;
}

const initialFormData: CreateConectorRequest = {
  systemApplicationId: '',
  integrationId: 0,
  name: '',
  isActive: true,
};

export default function ConectorFormModal({ open, onOpenChange, connector, onSuccess }: ConectorFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!connector;
  const [formData, setFormData] = useState<CreateConectorRequest>(initialFormData);
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
      loadIntegracoes();
    }
  }, [open]);

  useEffect(() => {
    if (connector) {
      setFormData({
        systemApplicationId: connector.systemApplicationId || '',
        integrationId: connector.integrationId || connector.integration?.id || 0,
        name: connector.name,
        isActive: connector.isActive,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [connector]);

  const loadIntegracoes = async () => {
    const result = await fetchIntegracoes(() => integracaoService.getActive());
    if (result) {
      setIntegrations(result);
    }
  };

  const handleChange = (field: keyof CreateConectorRequest, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => conectorService.update(connector.id, formData));
      } else {
        await execute(() => conectorService.create(formData));
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
          <ModalTitle>{isEditing ? t('connector.form.editTitle') : t('connector.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2 col-span-2">
              <label htmlFor="nome" className="text-sm font-medium">{t('common.column.name')}</label>
              <Input
                id="nome"
                value={formData.name}
                onChange={(e) => handleChange('name', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.column.integration')}</label>
              <Select
                value={formData.integrationId ? formData.integrationId.toString() : ''}
                onValueChange={(value) => handleChange('integrationId', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('connector.form.integrationPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {integrations.map((integracao) => (
                    <SelectItem key={integracao.id} value={integracao.id.toString()}>
                      {integracao.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label htmlFor="sistemaId" className="text-sm font-medium">{t('connector.form.systemId')}</label>
              <Input
                id="sistemaId"
                value={formData.systemApplicationId || ''}
                onChange={(e) => handleChange('systemApplicationId', e.target.value)}
              />
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox
              id="ativo"
              checked={formData.isActive}
              onCheckedChange={(checked) => handleChange('isActive', !!checked)}
            />
            <label htmlFor="ativo" className="text-sm font-medium">{t('common.column.active')}</label>
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
