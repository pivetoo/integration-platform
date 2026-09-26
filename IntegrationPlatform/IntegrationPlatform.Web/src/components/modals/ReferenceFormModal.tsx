import { useEffect, useState } from 'react';
import { Modal, ModalBody, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { referenceService } from '../../services/referenceService';
import { connectorService } from '../../services/connectorService';
import type { Reference, CreateReferenceRequest } from '../../types/reference';
import type { Conector } from '../../types/connector';

interface ReferenceFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  referencia: Reference | null;
  onSuccess: () => void;
}

const initialFormData: CreateReferenceRequest = {
  connectorId: 0,
  entity: '',
  internalId: '',
  externalId: '',
};

export default function ReferenceFormModal({ open, onOpenChange, referencia, onSuccess }: ReferenceFormModalProps) {
  const isEditing = !!referencia;
  const [formData, setFormData] = useState<CreateReferenceRequest>(initialFormData);
  const [conectores, setConectores] = useState<Conector[]>([]);
  const { t } = useI18n();

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchConectores } = useApi<Conector[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadConectores();
    }
  }, [open]);

  useEffect(() => {
    if (referencia) {
      setFormData({
        connectorId: referencia.connector?.id || 0,
        entity: referencia.entity,
        internalId: referencia.internalId,
        externalId: referencia.externalId,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [referencia]);

  const loadConectores = async () => {
    const result = await fetchConectores(() => connectorService.getActive());
    if (result) {
      setConectores(result);
    }
  };

  const handleChange = (field: keyof CreateReferenceRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const result = await execute(() => {
      if (isEditing) {
        const { connectorId: _, ...updateData } = formData;
        return referenceService.update(referencia.id, updateData);
      }

      return referenceService.create(formData);
    });

    if (result !== null) {
      onSuccess();
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('reference.form.editTitle') : t('reference.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-3 sm:gap-4">
          <ModalBody className="space-y-4">
            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('common.column.connector')}</label>
                <Select
                  value={formData.connectorId ? formData.connectorId.toString() : ''}
                  onValueChange={(value) => handleChange('connectorId', parseInt(value))}
                  disabled={isEditing}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('reference.form.connectorPlaceholder')} />
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
                <label htmlFor="entity" className="text-sm font-medium">{t('reference.form.entityLabel')}</label>
                <Input
                  id="entity"
                  value={formData.entity}
                  onChange={(e) => handleChange('entity', e.target.value)}
                  placeholder={t('reference.form.entityPlaceholder')}
                  required
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="internalId" className="text-sm font-medium">{t('reference.form.internalId')}</label>
                <Input
                  id="internalId"
                  value={formData.internalId}
                  onChange={(e) => handleChange('internalId', e.target.value)}
                  required
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="externalId" className="text-sm font-medium">{t('reference.form.externalId')}</label>
                <Input
                  id="externalId"
                  value={formData.externalId}
                  onChange={(e) => handleChange('externalId', e.target.value)}
                  required
                />
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
