import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, useApi, useI18n } from 'archon-ui';
import { integrationCategoryService } from '../../services/integrationCategoryService';
import type { IntegrationCategory, CreateIntegrationCategoryRequest } from '../../types/integrationCategory';

interface IntegrationCategoryFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  categoria: IntegrationCategory | null;
  onSuccess: () => void;
}

const initialFormData: CreateIntegrationCategoryRequest = {
  name: '',
  description: '',
};

export default function IntegrationCategoryFormModal({ open, onOpenChange, categoria, onSuccess }: IntegrationCategoryFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!categoria;
  const [formData, setFormData] = useState<CreateIntegrationCategoryRequest>(initialFormData);
  const [isActive, setIsActive] = useState(true);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (categoria) {
      setFormData({
        name: categoria.name,
        description: categoria.description || '',
      });
      setIsActive(categoria.isActive);
    } else {
      setFormData(initialFormData);
      setIsActive(true);
    }
  }, [categoria]);

  const handleChange = (field: keyof CreateIntegrationCategoryRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => integrationCategoryService.update(categoria.id, { ...formData, isActive }));
      } else {
        await execute(() => integrationCategoryService.create(formData));
      }
      onSuccess();
    } catch {
      // Error handled by useApi
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader>
          <ModalTitle>{isEditing ? t('integration.category.form.editTitle') : t('integration.category.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label htmlFor="name" className="text-sm font-medium">{t('common.column.name')}</label>
            <Input
              id="name"
              value={formData.name}
              onChange={(e) => handleChange('name', e.target.value)}
              required
              placeholder={t('integration.category.form.namePlaceholder')}
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="description" className="text-sm font-medium">{t('common.column.description')}</label>
            <Input
              id="description"
              value={formData.description || ''}
              onChange={(e) => handleChange('description', e.target.value)}
              placeholder={t('integration.category.form.descriptionPlaceholder')}
            />
          </div>

          {isEditing && (
            <div className="flex items-center space-x-2">
              <Checkbox
                id="isActive"
                checked={isActive}
                onCheckedChange={(checked) => setIsActive(checked as boolean)}
              />
              <label htmlFor="isActive" className="text-sm font-medium cursor-pointer">
                {t('common.column.active')}
              </label>
            </div>
          )}

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
