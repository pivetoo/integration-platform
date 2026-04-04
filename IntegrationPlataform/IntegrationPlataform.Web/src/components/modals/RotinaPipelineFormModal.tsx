import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Checkbox, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, toast } from 'archon-ui';
import { rotinaPipelineService } from '../../services/rotinaPipelineService';
import { conectorService } from '../../services/conectorService';
import { pipelineService } from '../../services/pipelineService';
import type { RotinaPipeline, CreateRotinaPipelineRequest, UpdateRotinaPipelineRequest } from '../../types/rotinaPipeline';
import type { Conector } from '../../types/conector';
import type { Pipeline } from '../../types/pipeline';

interface RotinaPipelineFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  rotina: RotinaPipeline | null;
  onSuccess: () => void;
}

const initialFormData: CreateRotinaPipelineRequest = {
  conectorId: 0,
  pipelineId: 0,
  ativo: true,
  intervaloMinutos: 60,
  payloadPadrao: '',
  proximaExecucao: '',
};

export default function RotinaPipelineFormModal({ open, onOpenChange, rotina, onSuccess }: RotinaPipelineFormModalProps) {
  const isEditing = !!rotina;
  const [formData, setFormData] = useState<CreateRotinaPipelineRequest>(initialFormData);
  const [conectores, setConectores] = useState<Conector[]>([]);
  const [pipelines, setPipelines] = useState<Pipeline[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({
        title: 'Sucesso',
        description: isEditing ? 'Rotina atualizada com sucesso' : 'Rotina criada com sucesso',
        variant: 'success',
      });
    },
  });

  const { execute: fetchConectores } = useApi<Conector[]>({ showErrorMessage: true });
  const { execute: fetchPipelines } = useApi<Pipeline[]>({ showErrorMessage: true });

  useEffect(() => {
    if (open) {
      loadConectores();
      loadPipelines();
    }
  }, [open]);

  useEffect(() => {
    if (rotina) {
      setFormData({
        conectorId: rotina.conector?.id || 0,
        pipelineId: rotina.pipeline?.id || 0,
        ativo: rotina.ativo,
        intervaloMinutos: rotina.intervaloMinutos,
        payloadPadrao: rotina.payloadPadrao || '',
        proximaExecucao: rotina.proximaExecucao ? rotina.proximaExecucao.slice(0, 16) : '',
      });
      return;
    }

    setFormData(initialFormData);
  }, [rotina]);

  const loadConectores = async () => {
    const result = await fetchConectores(() => conectorService.getAtivos());
    if (result) {
      setConectores(result);
    }
  };

  const loadPipelines = async () => {
    const result = await fetchPipelines(() => pipelineService.getAtivos());
    if (result) {
      setPipelines(result);
    }
  };

  const handleChange = (field: keyof CreateRotinaPipelineRequest, value: string | number | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (isEditing && rotina) {
        const payload: UpdateRotinaPipelineRequest = {
          ativo: formData.ativo,
          intervaloMinutos: formData.intervaloMinutos,
          payloadPadrao: formData.payloadPadrao || undefined,
          proximaExecucao: formData.proximaExecucao || undefined,
        };

        await execute(() => rotinaPipelineService.update(rotina.id, payload));
      } else {
        const payload: CreateRotinaPipelineRequest = {
          ...formData,
          payloadPadrao: formData.payloadPadrao || undefined,
          proximaExecucao: formData.proximaExecucao || undefined,
        };

        await execute(() => rotinaPipelineService.create(payload));
      }

      onSuccess();
    } catch {
      // handled by useApi
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar Rotina' : 'Nova Rotina'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Conector</label>
              <Select
                value={formData.conectorId ? formData.conectorId.toString() : ''}
                onValueChange={(value) => handleChange('conectorId', parseInt(value, 10))}
                disabled={isEditing}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione um conector" />
                </SelectTrigger>
                <SelectContent>
                  {conectores.map((conector) => (
                    <SelectItem key={conector.id} value={conector.id.toString()}>
                      {conector.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Pipeline</label>
              <Select
                value={formData.pipelineId ? formData.pipelineId.toString() : ''}
                onValueChange={(value) => handleChange('pipelineId', parseInt(value, 10))}
                disabled={isEditing}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione um pipeline" />
                </SelectTrigger>
                <SelectContent>
                  {pipelines.map((pipeline) => (
                    <SelectItem key={pipeline.id} value={pipeline.id.toString()}>
                      {pipeline.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label htmlFor="intervaloMinutos" className="text-sm font-medium">Intervalo (minutos)</label>
              <Input
                id="intervaloMinutos"
                type="number"
                min={1}
                value={formData.intervaloMinutos}
                onChange={(e) => handleChange('intervaloMinutos', parseInt(e.target.value, 10) || 1)}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="proximaExecucao" className="text-sm font-medium">Próxima execução</label>
              <Input
                id="proximaExecucao"
                type="datetime-local"
                value={formData.proximaExecucao || ''}
                onChange={(e) => handleChange('proximaExecucao', e.target.value)}
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="payloadPadrao" className="text-sm font-medium">Payload padrão (JSON)</label>
              <textarea
                id="payloadPadrao"
                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                rows={3}
                value={formData.payloadPadrao || ''}
                onChange={(e) => handleChange('payloadPadrao', e.target.value)}
              />
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
