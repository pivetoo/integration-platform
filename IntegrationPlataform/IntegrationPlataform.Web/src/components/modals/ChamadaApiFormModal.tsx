import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n } from 'archon-ui';
import { chamadaApiService } from '../../services/chamadaApiService';
import { HttpMethod, HttpMethodLabels } from '../../types/chamadaApi';
import type { ApiCall, CreateApiCallRequest } from '../../types/chamadaApi';

interface ChamadaApiFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  chamadaApi: ApiCall | null;
  onSuccess: () => void;
}

const initialFormData: CreateApiCallRequest = {
  name: '',
  description: '',
  method: HttpMethod.GET,
  url: '',
  headersTemplate: '',
  bodyTemplate: '',
};

export default function ChamadaApiFormModal({ open, onOpenChange, chamadaApi, onSuccess }: ChamadaApiFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!chamadaApi;
  const [formData, setFormData] = useState<CreateApiCallRequest>(initialFormData);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (chamadaApi) {
      setFormData({
        name: chamadaApi.name,
        description: chamadaApi.description || '',
        method: chamadaApi.method,
        url: chamadaApi.url,
        headersTemplate: chamadaApi.headersTemplate || '',
        bodyTemplate: chamadaApi.bodyTemplate || '',
      });
    } else {
      setFormData(initialFormData);
    }
  }, [chamadaApi]);

  const handleChange = (field: keyof CreateApiCallRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => chamadaApiService.update(chamadaApi.id, formData));
      } else {
        await execute(() => chamadaApiService.create(formData));
      }
      onSuccess();
    } catch {
      // Error handled by useApi
    }
  };

  const metodoOptions = Object.entries(HttpMethodLabels).map(([value, label]) => ({ value, label }));

  const showBody = formData.method !== HttpMethod.GET && formData.method !== HttpMethod.DELETE;

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="3xl">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('apiCall.form.editTitle') : t('apiCall.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2 col-span-2">
              <label htmlFor="name" className="text-sm font-medium">{t('common.column.name')}</label>
              <Input
                id="name"
                value={formData.name}
                onChange={(e) => handleChange('name', e.target.value)}
                placeholder=""
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
              <label className="text-sm font-medium">{t('common.column.method')}</label>
              <Select
                value={formData.method.toString()}
                onValueChange={(value) => handleChange('method', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {metodoOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="url" className="text-sm font-medium">{t('common.column.url')}</label>
              <Input
                id="url"
                value={formData.url}
                onChange={(e) => handleChange('url', e.target.value)}
                placeholder={t('apiCall.form.urlPlaceholder')}
                required
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="headersTemplate" className="text-sm font-medium">{t('apiCall.form.headersTemplate')}</label>
              <textarea
                id="headersTemplate"
                className="flex min-h-[60px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={formData.headersTemplate || ''}
                onChange={(e) => handleChange('headersTemplate', e.target.value)}
                placeholder={t('apiCall.form.headersPlaceholder')}
                rows={2}
              />
            </div>

            {showBody && (
              <div className="space-y-2 col-span-2">
                <label htmlFor="bodyTemplate" className="text-sm font-medium">{t('apiCall.form.bodyTemplate')}</label>
                <textarea
                  id="bodyTemplate"
                  className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  value={formData.bodyTemplate || ''}
                  onChange={(e) => handleChange('bodyTemplate', e.target.value)}
                  rows={3}
                />
              </div>
            )}
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
