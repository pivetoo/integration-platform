import { useEffect, useState } from 'react';
import { Modal, ModalBody, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Badge, useApi, useI18n } from 'archon-ui';
import { integrationCategoryService } from '../../services/integrationCategoryService';
import type { IntegrationCategory, CreateIntegrationCategoryRequest } from '../../types/integrationCategory';

interface IntegrationCategoryFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  categoria: IntegrationCategory | null;
  onSuccess: () => void;
}

const initialFormData: CreateIntegrationCategoryRequest = {
  identifier: '',
  name: '',
  description: '',
};

function slugifyIdentifier(value: string): string {
  return value
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

export default function IntegrationCategoryFormModal({ open, onOpenChange, categoria, onSuccess }: IntegrationCategoryFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!categoria;
  const isSystem = categoria?.isSystem ?? false;
  const [formData, setFormData] = useState<CreateIntegrationCategoryRequest>(initialFormData);
  const [identifierTouched, setIdentifierTouched] = useState(false);
  const [isActive, setIsActive] = useState(true);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (categoria) {
      setFormData({
        identifier: categoria.identifier,
        name: categoria.name,
        description: categoria.description || '',
      });
      setIsActive(categoria.isActive);
      setIdentifierTouched(true);
    } else {
      setFormData(initialFormData);
      setIsActive(true);
      setIdentifierTouched(false);
    }
  }, [categoria, open]);

  const handleNameChange = (value: string) => {
    setFormData((prev) => {
      const next = { ...prev, name: value };
      if (!isEditing && !identifierTouched) {
        next.identifier = slugifyIdentifier(value);
      }
      return next;
    });
  };

  const handleIdentifierChange = (value: string) => {
    setIdentifierTouched(true);
    setFormData((prev) => ({ ...prev, identifier: slugifyIdentifier(value) }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const result = await execute(() =>
      isEditing
        ? integrationCategoryService.update(categoria.id, {
            identifier: formData.identifier,
            name: formData.name,
            description: formData.description,
            isActive,
          })
        : integrationCategoryService.create(formData),
    );

    if (result !== null) {
      onSuccess();
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="md">
        <ModalHeader>
          <ModalTitle className="flex items-center gap-2">
            {isEditing ? t('integration.category.form.editTitle') : t('integration.category.form.createTitle')}
            {isSystem && <Badge variant="outline">{t('integration.category.form.systemBadge')}</Badge>}
          </ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-3 sm:gap-4">
          <ModalBody className="space-y-4">
            <div className="space-y-2">
              <label htmlFor="name" className="text-sm font-medium">{t('common.column.name')}</label>
              <Input
                id="name"
                value={formData.name}
                onChange={(e) => handleNameChange(e.target.value)}
                required
                placeholder={t('integration.category.form.namePlaceholder')}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="identifier" className="text-sm font-medium">
                {t('integration.category.form.identifier')}
              </label>
              <Input
                id="identifier"
                value={formData.identifier}
                onChange={(e) => handleIdentifierChange(e.target.value)}
                required
                disabled={isSystem}
                placeholder={t('integration.category.form.identifierPlaceholder')}
              />
              <p className="text-xs text-muted-foreground">
                {isSystem
                  ? t('integration.category.form.identifierSystemHint')
                  : t('integration.category.form.identifierHint')}
              </p>
            </div>

            <div className="space-y-2">
              <label htmlFor="description" className="text-sm font-medium">{t('common.column.description')}</label>
              <Input
                id="description"
                value={formData.description || ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))}
                placeholder={t('integration.category.form.descriptionPlaceholder')}
              />
            </div>
          </ModalBody>

          <ModalFooter>
            {isEditing && (
              <div className="flex items-center gap-2 sm:mr-auto">
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
