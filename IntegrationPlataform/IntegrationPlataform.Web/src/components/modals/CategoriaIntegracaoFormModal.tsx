import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, useApi, useI18n } from 'archon-ui';
import { categoriaIntegracaoService } from '../../services/categoriaIntegracaoService';
import type { CategoriaIntegracao, CreateCategoriaIntegracaoRequest } from '../../types/categoriaIntegracao';

interface CategoriaIntegracaoFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  categoria: CategoriaIntegracao | null;
  onSuccess: () => void;
}

const initialFormData: CreateCategoriaIntegracaoRequest = {
  nome: '',
  descricao: '',
};

export default function CategoriaIntegracaoFormModal({ open, onOpenChange, categoria, onSuccess }: CategoriaIntegracaoFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!categoria;
  const [formData, setFormData] = useState<CreateCategoriaIntegracaoRequest>(initialFormData);
  const [ativo, setAtivo] = useState(true);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (categoria) {
      setFormData({
        nome: categoria.nome,
        descricao: categoria.descricao || '',
      });
      setAtivo(categoria.ativo);
    } else {
      setFormData(initialFormData);
      setAtivo(true);
    }
  }, [categoria]);

  const handleChange = (field: keyof CreateCategoriaIntegracaoRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => categoriaIntegracaoService.update(categoria.id, { ...formData, ativo }));
      } else {
        await execute(() => categoriaIntegracaoService.create(formData));
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
            <label htmlFor="nome" className="text-sm font-medium">{t('common.column.name')}</label>
            <Input
              id="nome"
              value={formData.nome}
              onChange={(e) => handleChange('nome', e.target.value)}
              required
              placeholder={t('integration.category.form.namePlaceholder')}
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="descricao" className="text-sm font-medium">{t('common.column.description')}</label>
            <Input
              id="descricao"
              value={formData.descricao || ''}
              onChange={(e) => handleChange('descricao', e.target.value)}
              placeholder={t('integration.category.form.descriptionPlaceholder')}
            />
          </div>

          {isEditing && (
            <div className="flex items-center space-x-2">
              <Checkbox
                id="ativo"
                checked={ativo}
                onCheckedChange={(checked) => setAtivo(checked as boolean)}
              />
              <label htmlFor="ativo" className="text-sm font-medium cursor-pointer">
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
