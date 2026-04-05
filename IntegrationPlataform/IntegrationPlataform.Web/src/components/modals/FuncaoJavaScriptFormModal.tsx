import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, useApi, useI18n } from 'archon-ui';
import { funcaoJavaScriptService } from '../../services/funcaoJavaScriptService';
import type { FuncaoJavaScript, CreateFuncaoJavaScriptRequest } from '../../types/funcaoJavaScript';

interface FuncaoJavaScriptFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  funcaoJavaScript: FuncaoJavaScript | null;
  onSuccess: () => void;
}

const initialFormData: CreateFuncaoJavaScriptRequest = {
  nome: '',
  descricao: '',
  codigo: '',
};

export default function FuncaoJavaScriptFormModal({ open, onOpenChange, funcaoJavaScript, onSuccess }: FuncaoJavaScriptFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!funcaoJavaScript;
  const [formData, setFormData] = useState<CreateFuncaoJavaScriptRequest>(initialFormData);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (funcaoJavaScript) {
      setFormData({
        nome: funcaoJavaScript.nome,
        descricao: funcaoJavaScript.descricao || '',
        codigo: funcaoJavaScript.codigo,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [funcaoJavaScript]);

  const handleChange = (field: keyof CreateFuncaoJavaScriptRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => funcaoJavaScriptService.update(funcaoJavaScript.id, formData));
      } else {
        await execute(() => funcaoJavaScriptService.create(formData));
      }
      onSuccess();
    } catch {
      // Error handled by useApi
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="3xl">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('javaScriptFunction.form.editTitle') : t('javaScriptFunction.form.createTitle')}</ModalTitle>
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

            <div className="space-y-2 col-span-2">
              <label htmlFor="descricao" className="text-sm font-medium">{t('common.column.description')}</label>
              <Input
                id="descricao"
                value={formData.descricao || ''}
                onChange={(e) => handleChange('descricao', e.target.value)}
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="codigo" className="text-sm font-medium">{t('javaScriptFunction.form.codeLabel')}</label>
              <textarea
                id="codigo"
                className="flex min-h-[200px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={formData.codigo}
                onChange={(e) => handleChange('codigo', e.target.value)}
                required
                rows={8}
              />
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
