import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui';
import { pipelineService } from '../../services/pipelineService';
import { integracaoService } from '../../services/integracaoService';
import type { Pipeline, CreatePipelineRequest } from '../../types/pipeline';
import type { Integracao } from '../../types/integracao';

interface PipelineFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  pipeline: Pipeline | null;
  onSuccess: () => void;
}

const initialFormData: CreatePipelineRequest = {
  integracaoId: 0,
  identificador: '',
  nome: '',
  descricao: '',
  ativo: true,
};

export default function PipelineFormModal({ open, onOpenChange, pipeline, onSuccess }: PipelineFormModalProps) {
  const isEditing = !!pipeline;
  const [formData, setFormData] = useState<CreatePipelineRequest>(initialFormData);
  const [integracoes, setIntegracoes] = useState<Integracao[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchIntegracoes } = useApi<Integracao[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadIntegracoes();
    }
  }, [open]);

  useEffect(() => {
    if (pipeline) {
      setFormData({
        integracaoId: pipeline.integracao?.id || 0,
        identificador: pipeline.identificador,
        nome: pipeline.nome,
        descricao: pipeline.descricao || '',
        ativo: pipeline.ativo,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [pipeline]);

  const loadIntegracoes = async () => {
    const result = await fetchIntegracoes(() => integracaoService.getAtivas());
    if (result) {
      setIntegracoes(result);
    }
  };

  const handleChange = (field: keyof CreatePipelineRequest, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => pipelineService.update(pipeline.id, formData));
      } else {
        await execute(() => pipelineService.create(formData));
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
          <ModalTitle>{isEditing ? 'Editar Pipeline' : 'Novo Pipeline'}</ModalTitle>
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
              <label className="text-sm font-medium">Integração</label>
              <Select
                value={formData.integracaoId ? formData.integracaoId.toString() : ''}
                onValueChange={(value) => handleChange('integracaoId', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione uma integração" />
                </SelectTrigger>
                <SelectContent>
                  {integracoes.map((integracao) => (
                    <SelectItem key={integracao.id} value={integracao.id.toString()}>
                      {integracao.nome}
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
            <label htmlFor="ativo" className="text-sm font-medium">Ativo</label>
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
