import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, useApi } from 'archon-ui';
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
          <ModalTitle>{isEditing ? 'Editar Categoria' : 'Nova Categoria'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label htmlFor="nome" className="text-sm font-medium">Nome</label>
            <Input
              id="nome"
              value={formData.nome}
              onChange={(e) => handleChange('nome', e.target.value)}
              required
              placeholder="Ex: Marketplace"
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="descricao" className="text-sm font-medium">Descrição</label>
            <Input
              id="descricao"
              value={formData.descricao || ''}
              onChange={(e) => handleChange('descricao', e.target.value)}
              placeholder="Ex: Integrações com marketplaces"
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
                Ativo
              </label>
            </div>
          )}

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Salvando...' : 'Salvar'}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  );
}
