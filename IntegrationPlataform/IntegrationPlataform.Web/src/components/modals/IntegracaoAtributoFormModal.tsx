import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'd-rts';
import { integracaoAtributoService } from '../../services/integracaoAtributoService';
import { TipoCampo, TipoCampoLabels } from '../../types/integracaoAtributo';
import type { IntegracaoAtributo, CreateIntegracaoAtributoRequest } from '../../types/integracaoAtributo';

interface IntegracaoAtributoFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  integracaoId: number;
  atributo: IntegracaoAtributo | null;
  nextOrdem: number;
  onSuccess: () => void;
}

const initialFormData: Omit<CreateIntegracaoAtributoRequest, 'integracaoId'> = {
  campo: '',
  label: '',
  descricao: '',
  placeholder: '',
  tipo: TipoCampo.Texto,
  valorPadrao: '',
  obrigatorio: false,
  ordem: 1,
  grupo: '',
  sensivel: false,
};

export default function IntegracaoAtributoFormModal({ open, onOpenChange, integracaoId, atributo, nextOrdem, onSuccess }: IntegracaoAtributoFormModalProps) {
  const isEditing = !!atributo;
  const [formData, setFormData] = useState(initialFormData);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (atributo) {
      setFormData({
        campo: atributo.campo,
        label: atributo.label,
        descricao: atributo.descricao || '',
        placeholder: atributo.placeholder || '',
        tipo: atributo.tipo,
        valorPadrao: atributo.valorPadrao || '',
        obrigatorio: atributo.obrigatorio,
        ordem: atributo.ordem,
        grupo: atributo.grupo || '',
        sensivel: atributo.sensivel,
      });
    } else {
      setFormData({ ...initialFormData, ordem: nextOrdem });
    }
  }, [atributo, nextOrdem]);

  const handleChange = (field: string, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => integracaoAtributoService.update(atributo.id, {
          campo: formData.campo,
          label: formData.label,
          descricao: formData.descricao || undefined,
          placeholder: formData.placeholder || undefined,
          tipo: formData.tipo,
          valorPadrao: formData.valorPadrao || undefined,
          obrigatorio: formData.obrigatorio,
          ordem: formData.ordem,
          grupo: formData.grupo || undefined,
          sensivel: formData.sensivel,
        }));
      } else {
        await execute(() => integracaoAtributoService.create({
          integracaoId,
          campo: formData.campo,
          label: formData.label,
          descricao: formData.descricao || undefined,
          placeholder: formData.placeholder || undefined,
          tipo: formData.tipo,
          valorPadrao: formData.valorPadrao || undefined,
          obrigatorio: formData.obrigatorio,
          ordem: formData.ordem,
          grupo: formData.grupo || undefined,
          sensivel: formData.sensivel,
        }));
      }
      onSuccess();
    } catch {
      // Error handled by useApi
    }
  };

  const tipoCampoOptions = Object.entries(TipoCampoLabels).map(([value, label]) => ({
    value,
    label,
  }));

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar Atributo' : 'Novo Atributo'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="campo" className="text-sm font-medium">Campo</label>
              <Input
                id="campo"
                value={formData.campo}
                onChange={(e) => handleChange('campo', e.target.value)}
                placeholder="nome_do_campo"
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="label" className="text-sm font-medium">Label</label>
              <Input
                id="label"
                value={formData.label}
                onChange={(e) => handleChange('label', e.target.value)}
                placeholder="Nome de exibição"
                required
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="descricao" className="text-sm font-medium">Descrição</label>
              <Input
                id="descricao"
                value={formData.descricao || ''}
                onChange={(e) => handleChange('descricao', e.target.value)}
                placeholder="Descrição do atributo"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
              <Select
                value={formData.tipo.toString()}
                onValueChange={(value) => handleChange('tipo', parseInt(value))}
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
              <label htmlFor="placeholder" className="text-sm font-medium">Placeholder</label>
              <Input
                id="placeholder"
                value={formData.placeholder || ''}
                onChange={(e) => handleChange('placeholder', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="valorPadrao" className="text-sm font-medium">Valor Padrão</label>
              <Input
                id="valorPadrao"
                value={formData.valorPadrao || ''}
                onChange={(e) => handleChange('valorPadrao', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="grupo" className="text-sm font-medium">Grupo</label>
              <Input
                id="grupo"
                value={formData.grupo || ''}
                onChange={(e) => handleChange('grupo', e.target.value)}
                placeholder="Ex: Autenticação, Configuração"
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="ordem" className="text-sm font-medium">Ordem</label>
              <Input
                id="ordem"
                type="number"
                value={formData.ordem}
                onChange={(e) => handleChange('ordem', parseInt(e.target.value) || 0)}
                min={1}
              />
            </div>
          </div>

          <div className="flex items-center gap-6">
            <div className="flex items-center space-x-2">
              <Checkbox
                id="obrigatorio"
                checked={formData.obrigatorio}
                onCheckedChange={(checked) => handleChange('obrigatorio', !!checked)}
              />
              <label htmlFor="obrigatorio" className="text-sm font-medium">Obrigatório</label>
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox
                id="sensivel"
                checked={formData.sensivel}
                onCheckedChange={(checked) => handleChange('sensivel', !!checked)}
              />
              <label htmlFor="sensivel" className="text-sm font-medium">Sensível</label>
            </div>
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
