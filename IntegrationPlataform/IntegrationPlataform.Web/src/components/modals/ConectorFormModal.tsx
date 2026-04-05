import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { conectorService } from '../../services/conectorService';
import { integracaoService } from '../../services/integracaoService';
import type { Conector, CreateConectorRequest } from '../../types/conector';
import type { Integracao } from '../../types/integracao';

interface ConectorFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  conector: Conector | null;
  onSuccess: () => void;
}

const initialFormData: CreateConectorRequest = {
  sistemaId: '',
  integracaoId: 0,
  nome: '',
  ativo: true,
};

export default function ConectorFormModal({ open, onOpenChange, conector, onSuccess }: ConectorFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!conector;
  const [formData, setFormData] = useState<CreateConectorRequest>(initialFormData);
  const [integracoes, setIntegracoes] = useState<Integracao[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchIntegracoes } = useApi<Integracao[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadIntegracoes();
    }
  }, [open]);

  useEffect(() => {
    if (conector) {
      setFormData({
        sistemaId: conector.sistemaId || '',
        integracaoId: conector.integracaoId || conector.integracao?.id || 0,
        nome: conector.nome,
        ativo: conector.ativo,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [conector]);

  const loadIntegracoes = async () => {
    const result = await fetchIntegracoes(() => integracaoService.getAtivas());
    if (result) {
      setIntegracoes(result);
    }
  };

  const handleChange = (field: keyof CreateConectorRequest, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => conectorService.update(conector.id, formData));
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
                value={formData.nome}
                onChange={(e) => handleChange('nome', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.column.integration')}</label>
              <Select
                value={formData.integracaoId ? formData.integracaoId.toString() : ''}
                onValueChange={(value) => handleChange('integracaoId', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('connector.form.integrationPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {integracoes.map((integracao) => (
                    <SelectItem key={integracao.id} value={integracao.id.toString()}>
                      {integracao.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label htmlFor="sistemaId" className="text-sm font-medium">{t('connector.form.systemId')}</label>
              <Input
                id="sistemaId"
                value={formData.sistemaId || ''}
                onChange={(e) => handleChange('sistemaId', e.target.value)}
              />
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox
              id="ativo"
              checked={formData.ativo}
              onCheckedChange={(checked) => handleChange('ativo', !!checked)}
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
