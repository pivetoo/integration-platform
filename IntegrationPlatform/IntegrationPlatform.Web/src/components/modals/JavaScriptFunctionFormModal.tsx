import { useEffect, useState } from 'react';
import { Modal, ModalBody, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, useApi, useI18n } from 'archon-ui';
import { javaScriptFunctionService } from '../../services/javaScriptFunctionService';
import type { JavaScriptFunction, CreateJavaScriptFunctionRequest } from '../../types/javaScriptFunction';

interface JavaScriptFunctionFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  funcaoJavaScript: JavaScriptFunction | null;
  onSuccess: () => void;
}

const initialFormData: CreateJavaScriptFunctionRequest = {
  name: '',
  description: '',
  code: '',
};

export default function JavaScriptFunctionFormModal({ open, onOpenChange, funcaoJavaScript, onSuccess }: JavaScriptFunctionFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!funcaoJavaScript;
  const [formData, setFormData] = useState<CreateJavaScriptFunctionRequest>(initialFormData);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (funcaoJavaScript) {
      setFormData({
        name: funcaoJavaScript.name,
        description: funcaoJavaScript.description || '',
        code: funcaoJavaScript.code,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [funcaoJavaScript]);

  const handleChange = (field: keyof CreateJavaScriptFunctionRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const result = await execute(() =>
      isEditing
        ? javaScriptFunctionService.update(funcaoJavaScript.id, formData)
        : javaScriptFunctionService.create(formData)
    );

    if (result !== null) {
      onSuccess();
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="3xl">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('javaScriptFunction.form.editTitle') : t('javaScriptFunction.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-3 sm:gap-4">
          <ModalBody className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2 col-span-2">
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

              <div className="space-y-2 col-span-2">
                <label htmlFor="code" className="text-sm font-medium">{t('javaScriptFunction.form.codeLabel')}</label>
                <textarea
                  id="code"
                  className="flex min-h-[200px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  value={formData.code}
                  onChange={(e) => handleChange('code', e.target.value)}
                  required
                  rows={8}
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
