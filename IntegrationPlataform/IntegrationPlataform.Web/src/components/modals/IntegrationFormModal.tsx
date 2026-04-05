import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { integrationService } from '../../services/integrationService';
import { integrationCategoryService } from '../../services/integrationCategoryService';
import type { Integration, CreateIntegrationRequest } from '../../types/integration';
import type { IntegrationCategory } from '../../types/integrationCategory';

interface IntegrationFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  integracao: Integration | null;
  onSuccess: () => void;
}

const initialFormData: CreateIntegrationRequest = {
  identifier: '',
  name: '',
  description: '',
  integrationCategoryId: undefined,
  isActive: true,
};

export default function IntegrationFormModal({ open, onOpenChange, integracao, onSuccess }: IntegrationFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!integracao;
  const [formData, setFormData] = useState<CreateIntegrationRequest>(initialFormData);
  const [categorias, setCategorias] = useState<IntegrationCategory[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchCategorias } = useApi<IntegrationCategory[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadCategorias();
    }
  }, [open]);

  useEffect(() => {
    if (integracao) {
      setFormData({
        identifier: integracao.identifier,
        name: integracao.name,
        description: integracao.description || '',
        integrationCategoryId: integracao.integrationCategoryId ?? integracao.integrationCategory?.id,
        isActive: integracao.isActive,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [integracao]);

  const loadCategorias = async () => {
    const result = await fetchCategorias(() => integrationCategoryService.getActive());
    if (result) {
      setCategorias(result);
    }
  };

  const handleChange = (field: keyof CreateIntegrationRequest, value: string | number | boolean | undefined) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => integrationService.update(integracao.id, formData));
      } else {
        await execute(() => integrationService.create(formData));
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
          <ModalTitle>{isEditing ? t('integration.form.editTitle') : t('integration.form.createTitle')}</ModalTitle>
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
              <label className="text-sm font-medium">{t('common.column.category')}</label>
              <Select
                value={formData.integrationCategoryId ? formData.integrationCategoryId.toString() : '_none'}
                onValueChange={(value) => handleChange('integrationCategoryId', value === '_none' ? undefined : parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('integration.form.categoryPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none">{t('integration.form.noCategory')}</SelectItem>
                  {categorias.map((categoria) => (
                    <SelectItem key={categoria.id} value={categoria.id.toString()}>
                      {categoria.name}
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
            <label htmlFor="isActive" className="text-sm font-medium cursor-pointer">{t('common.column.active')}</label>
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
