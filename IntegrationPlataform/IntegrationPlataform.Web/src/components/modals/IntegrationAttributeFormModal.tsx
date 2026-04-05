import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { integrationAttributeService } from '../../services/integrationAttributeService';
import { TipoCampo } from '../../types/integrationAttribute';
import type { IntegracaoAtributo, CreateIntegracaoAtributoRequest } from '../../types/integrationAttribute';

interface IntegrationAttributeFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  integrationId: number;
  attribute: IntegracaoAtributo | null;
  nextOrder: number;
  onSuccess: () => void;
}

const initialFormData: Omit<CreateIntegracaoAtributoRequest, 'integrationId'> = {
  field: '',
  label: '',
  description: '',
  placeholder: '',
  type: TipoCampo.Texto,
  defaultValue: '',
  isRequired: false,
  order: 1,
  group: '',
  isSensitive: false,
};

export default function IntegrationAttributeFormModal({ open, onOpenChange, integrationId, attribute, nextOrder, onSuccess }: IntegrationAttributeFormModalProps) {
  const isEditing = !!attribute;
  const [formData, setFormData] = useState(initialFormData);
  const { t } = useI18n();

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (attribute) {
      setFormData({
        field: attribute.field,
        label: attribute.label,
        description: attribute.description || '',
        placeholder: attribute.placeholder || '',
        type: attribute.type,
        defaultValue: attribute.defaultValue || '',
        isRequired: attribute.isRequired,
        order: attribute.order,
        group: attribute.group || '',
        isSensitive: attribute.isSensitive,
      });
    } else {
      setFormData({ ...initialFormData, order: nextOrder });
    }
  }, [attribute, nextOrder]);

  const handleChange = (field: string, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => integrationAttributeService.update(attribute.id, {
          field: formData.field,
          label: formData.label,
          description: formData.description || undefined,
          placeholder: formData.placeholder || undefined,
          type: formData.type,
          defaultValue: formData.defaultValue || undefined,
          isRequired: formData.isRequired,
          order: formData.order,
          group: formData.group || undefined,
          isSensitive: formData.isSensitive,
        }));
      } else {
        await execute(() => integrationAttributeService.create({
          integrationId,
          field: formData.field,
          label: formData.label,
          description: formData.description || undefined,
          placeholder: formData.placeholder || undefined,
          type: formData.type,
          defaultValue: formData.defaultValue || undefined,
          isRequired: formData.isRequired,
          order: formData.order,
          group: formData.group || undefined,
          isSensitive: formData.isSensitive,
        }));
      }
      onSuccess();
    } catch {
      // Error handled by useApi
    }
  };

  const tipoCampoOptions = [
    { value: TipoCampo.Texto.toString(), label: t('integration.attribute.type.text') },
    { value: TipoCampo.TextoLongo.toString(), label: t('integration.attribute.type.longText') },
    { value: TipoCampo.Numero.toString(), label: t('integration.attribute.type.number') },
    { value: TipoCampo.Decimal.toString(), label: t('integration.attribute.type.decimal') },
    { value: TipoCampo.Booleano.toString(), label: t('integration.attribute.type.boolean') },
    { value: TipoCampo.Data.toString(), label: t('integration.attribute.type.date') },
    { value: TipoCampo.DataHora.toString(), label: t('integration.attribute.type.dateTime') },
    { value: TipoCampo.Lista.toString(), label: t('integration.attribute.type.list') },
  ];

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('integration.attribute.form.editTitle') : t('integration.attribute.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="campo" className="text-sm font-medium">{t('integration.attribute.form.fieldLabel')}</label>
              <Input
                id="campo"
                value={formData.field}
                onChange={(e) => handleChange('field', e.target.value)}
                placeholder={t('integration.attribute.form.fieldPlaceholder')}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="label" className="text-sm font-medium">{t('integration.attribute.form.labelLabel')}</label>
              <Input
                id="label"
                value={formData.label}
                onChange={(e) => handleChange('label', e.target.value)}
                placeholder={t('integration.attribute.form.labelPlaceholder')}
                required
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="descricao" className="text-sm font-medium">{t('common.column.description')}</label>
              <Input
                id="descricao"
                value={formData.description || ''}
                onChange={(e) => handleChange('description', e.target.value)}
                placeholder={t('integration.attribute.form.descriptionPlaceholder')}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('common.column.type')}</label>
              <Select
                value={formData.type.toString()}
                onValueChange={(value) => handleChange('type', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {tipoCampoOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label htmlFor="placeholder" className="text-sm font-medium">{t('integration.attribute.form.placeholderLabel')}</label>
              <Input
                id="placeholder"
                value={formData.placeholder || ''}
                onChange={(e) => handleChange('placeholder', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="valorPadrao" className="text-sm font-medium">{t('integration.attribute.form.defaultValueLabel')}</label>
              <Input
                id="valorPadrao"
                value={formData.defaultValue || ''}
                onChange={(e) => handleChange('defaultValue', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="grupo" className="text-sm font-medium">{t('integration.attribute.form.groupLabel')}</label>
              <Input
                id="grupo"
                value={formData.group || ''}
                onChange={(e) => handleChange('group', e.target.value)}
                placeholder={t('integration.attribute.form.groupPlaceholder')}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="ordem" className="text-sm font-medium">{t('common.column.order')}</label>
              <Input
                id="ordem"
                type="number"
                value={formData.order}
                onChange={(e) => handleChange('order', parseInt(e.target.value) || 0)}
                min={1}
              />
            </div>
          </div>

          <div className="flex items-center gap-6">
            <div className="flex items-center space-x-2">
              <Checkbox
                id="obrigatorio"
                checked={formData.isRequired}
                onCheckedChange={(checked) => handleChange('isRequired', !!checked)}
              />
              <label htmlFor="obrigatorio" className="text-sm font-medium">{t('integration.detail.required')}</label>
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox
                id="sensivel"
                checked={formData.isSensitive}
                onCheckedChange={(checked) => handleChange('isSensitive', !!checked)}
              />
              <label htmlFor="sensivel" className="text-sm font-medium">{t('integration.detail.sensitive')}</label>
            </div>
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
