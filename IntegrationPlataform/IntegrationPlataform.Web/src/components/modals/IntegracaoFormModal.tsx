import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui';
import { integracaoService } from '../../services/integracaoService';
import { categoriaIntegracaoService } from '../../services/categoriaIntegracaoService';
import type { Integracao, CreateIntegracaoRequest } from '../../types/integracao';
import type { CategoriaIntegracao } from '../../types/categoriaIntegracao';

interface IntegracaoFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  integracao: Integracao | null;
  onSuccess: () => void;
}

const initialFormData: CreateIntegracaoRequest = {
  identificador: '',
  nome: '',
  descricao: '',
  categoriaId: undefined,
  ativo: true,
};

export default function IntegracaoFormModal({ open, onOpenChange, integracao, onSuccess }: IntegracaoFormModalProps) {
  const isEditing = !!integracao;
  const [formData, setFormData] = useState<CreateIntegracaoRequest>(initialFormData);
  const [categorias, setCategorias] = useState<CategoriaIntegracao[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchCategorias } = useApi<CategoriaIntegracao[]>({
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
        identificador: integracao.identificador,
        nome: integracao.nome,
        descricao: integracao.descricao || '',
        categoriaId: integracao.categoria?.id,
        ativo: integracao.ativo,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [integracao]);

  const loadCategorias = async () => {
    const result = await fetchCategorias(() => categoriaIntegracaoService.getAtivas());
    if (result) {
      setCategorias(result);
    }
  };

  const handleChange = (field: keyof CreateIntegracaoRequest, value: string | number | boolean | undefined) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => integracaoService.update(integracao.id, formData));
      } else {
        await execute(() => integracaoService.create(formData));
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
          <ModalTitle>{isEditing ? 'Editar Integração' : 'Nova Integração'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="identificador" className="text-sm font-medium">Identificador</label>
              <Input
                id="identificador"
                value={formData.identificador}
                onChange={(e) => handleChange('identificador', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="nome" className="text-sm font-medium">Nome</label>
              <Input
                id="nome"
                value={formData.nome}
                onChange={(e) => handleChange('nome', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="descricao" className="text-sm font-medium">Descrição</label>
              <Input
                id="descricao"
                value={formData.descricao || ''}
                onChange={(e) => handleChange('descricao', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Categoria</label>
              <Select
                value={formData.categoriaId ? formData.categoriaId.toString() : '_none'}
                onValueChange={(value) => handleChange('categoriaId', value === '_none' ? undefined : parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione uma categoria" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none">Sem categoria</SelectItem>
                  {categorias.map((categoria) => (
                    <SelectItem key={categoria.id} value={categoria.id.toString()}>
                      {categoria.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox
              id="ativo"
              checked={formData.ativo}
              onCheckedChange={(checked) => handleChange('ativo', !!checked)}
            />
            <label htmlFor="ativo" className="text-sm font-medium cursor-pointer">Ativo</label>
          </div>

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
