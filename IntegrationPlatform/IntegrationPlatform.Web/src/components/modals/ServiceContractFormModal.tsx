import { useEffect, useState } from 'react';
import { Modal, ModalBody, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Badge, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { serviceContractService } from '../../services/serviceContractService';
import { integrationCategoryService } from '../../services/integrationCategoryService';
import type { ServiceContract, CreateServiceContractRequest } from '../../types/serviceContract';
import type { IntegrationCategory } from '../../types/integrationCategory';

interface ServiceContractFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  serviceContract: ServiceContract | null;
  onSuccess: () => void;
}

const initialFormData: CreateServiceContractRequest = {
  identifier: '',
  name: '',
  description: '',
  integrationCategoryId: 0,
  inputSchema: '',
  outputSchema: '',
  hasCallback: false,
  callbackSchema: '',
};

const textareaClass = 'flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2';

function normalizeIdentifier(value: string): string {
  return value.toLowerCase().replace(/[^a-z0-9.\-]+/g, '');
}

export default function ServiceContractFormModal({ open, onOpenChange, serviceContract, onSuccess }: ServiceContractFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!serviceContract;
  const isSystem = serviceContract?.isSystem ?? false;
  const [formData, setFormData] = useState<CreateServiceContractRequest>(initialFormData);
  const [isActive, setIsActive] = useState(true);
  const [categories, setCategories] = useState<IntegrationCategory[]>([]);

  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true });
  const { execute: fetchCategories } = useApi<IntegrationCategory[]>({ showErrorMessage: true });

  useEffect(() => {
    if (open) {
      void fetchCategories(() => integrationCategoryService.getActive()).then((result) => {
        if (result) {
          setCategories(result);
        }
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  useEffect(() => {
    if (serviceContract) {
      setFormData({
        identifier: serviceContract.identifier,
        name: serviceContract.name,
        description: serviceContract.description || '',
        integrationCategoryId: serviceContract.integrationCategoryId,
        inputSchema: serviceContract.inputSchema || '',
        outputSchema: serviceContract.outputSchema || '',
        hasCallback: serviceContract.hasCallback,
        callbackSchema: serviceContract.callbackSchema || '',
      });
      setIsActive(serviceContract.isActive);
    } else {
      setFormData(initialFormData);
      setIsActive(true);
    }
  }, [serviceContract, open]);

  const handleChange = (field: keyof CreateServiceContractRequest, value: string | number | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload: CreateServiceContractRequest = {
      ...formData,
      callbackSchema: formData.hasCallback ? formData.callbackSchema : '',
    };
    const result = await execute(() =>
      isEditing
        ? serviceContractService.update(serviceContract.id, { ...payload, isActive })
        : serviceContractService.create(payload),
    );
    if (result !== null) {
      onSuccess();
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="3xl">
        <ModalHeader>
          <ModalTitle className="flex items-center gap-2">
            {isEditing ? t('serviceContract.form.editTitle') : t('serviceContract.form.createTitle')}
            {isSystem && <Badge variant="outline">{t('serviceContract.form.systemBadge')}</Badge>}
          </ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-3 sm:gap-4">
          <ModalBody className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="name" className="text-sm font-medium">{t('common.column.name')}</label>
                <Input id="name" value={formData.name} onChange={(e) => handleChange('name', e.target.value)} required />
              </div>

              <div className="space-y-2">
                <label htmlFor="identifier" className="text-sm font-medium">{t('serviceContract.form.identifier')}</label>
                <Input
                  id="identifier"
                  value={formData.identifier}
                  onChange={(e) => handleChange('identifier', normalizeIdentifier(e.target.value))}
                  required
                  disabled={isSystem}
                  placeholder="receivable.charge.create"
                />
                <p className="text-xs text-muted-foreground">
                  {isSystem ? t('serviceContract.form.identifierSystemHint') : t('serviceContract.form.identifierHint')}
                </p>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">{t('serviceContract.form.category')}</label>
                <Select
                  value={formData.integrationCategoryId ? formData.integrationCategoryId.toString() : ''}
                  onValueChange={(value) => handleChange('integrationCategoryId', Number(value))}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('serviceContract.form.categoryPlaceholder')} />
                  </SelectTrigger>
                  <SelectContent>
                    {categories.map((category) => (
                      <SelectItem key={category.id} value={category.id.toString()}>{category.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex items-center gap-2 pt-7">
                <Checkbox id="hasCallback" checked={formData.hasCallback} onCheckedChange={(checked) => handleChange('hasCallback', checked as boolean)} />
                <label htmlFor="hasCallback" className="text-sm font-medium cursor-pointer">{t('serviceContract.form.hasCallback')}</label>
              </div>

              <div className="space-y-2 col-span-2">
                <label htmlFor="description" className="text-sm font-medium">{t('common.column.description')}</label>
                <Input id="description" value={formData.description || ''} onChange={(e) => handleChange('description', e.target.value)} />
              </div>

              <div className="space-y-2 col-span-2">
                <label htmlFor="inputSchema" className="text-sm font-medium">{t('serviceContract.form.inputSchema')}</label>
                <textarea id="inputSchema" className={textareaClass} value={formData.inputSchema || ''} onChange={(e) => handleChange('inputSchema', e.target.value)} rows={6} />
              </div>

              <div className="space-y-2 col-span-2">
                <label htmlFor="outputSchema" className="text-sm font-medium">{t('serviceContract.form.outputSchema')}</label>
                <textarea id="outputSchema" className={textareaClass} value={formData.outputSchema || ''} onChange={(e) => handleChange('outputSchema', e.target.value)} rows={4} />
              </div>

              {formData.hasCallback && (
                <div className="space-y-2 col-span-2">
                  <label htmlFor="callbackSchema" className="text-sm font-medium">{t('serviceContract.form.callbackSchema')}</label>
                  <textarea id="callbackSchema" className={textareaClass} value={formData.callbackSchema || ''} onChange={(e) => handleChange('callbackSchema', e.target.value)} rows={4} />
                </div>
              )}
            </div>
          </ModalBody>

          <ModalFooter>
            {isEditing && (
              <div className="flex items-center gap-2 sm:mr-auto">
                <Checkbox id="isActive" checked={isActive} onCheckedChange={(checked) => setIsActive(checked as boolean)} />
                <label htmlFor="isActive" className="text-sm font-medium cursor-pointer">{t('common.column.active')}</label>
              </div>
            )}
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
            <Button type="submit" disabled={loading || !formData.integrationCategoryId}>
              {loading ? t('common.action.saving') : t('common.action.save')}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  );
}
