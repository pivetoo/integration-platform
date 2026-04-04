import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'd-rts';
import { filaProcessamentoService } from '../../services/filaProcessamentoService';
import { conectorService } from '../../services/conectorService';
import { pipelineService } from '../../services/pipelineService';
import type { CreateFilaProcessamentoRequest } from '../../types/filaProcessamento';
import type { Conector } from '../../types/conector';
import type { Pipeline } from '../../types/pipeline';

interface FilaFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

const initialFormData: CreateFilaProcessamentoRequest = {
  conectorId: 0,
  pipelineId: 0,
  prioridade: 0,
  payload: '',
  agendamento: '',
};

export default function FilaFormModal({ open, onOpenChange, onSuccess }: FilaFormModalProps) {
  const [formData, setFormData] = useState<CreateFilaProcessamentoRequest>(initialFormData);
  const [conectores, setConectores] = useState<Conector[]>([]);
  const [pipelines, setPipelines] = useState<Pipeline[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchConectores } = useApi<Conector[]>({
    showErrorMessage: true,
  });

  const { execute: fetchPipelines } = useApi<Pipeline[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadConectores();
      loadPipelines();
      setFormData(initialFormData);
    }
  }, [open]);

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

  const handleChange = (field: keyof CreateFilaProcessamentoRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const data = {
        ...formData,
        agendamento: formData.agendamento || undefined,
        payload: formData.payload || undefined,
      };
      await execute(() => filaProcessamentoService.create(data));
      onSuccess();
    } catch {
      // Error handled by useApi
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent>
        <ModalHeader>
          <ModalTitle>Enfileirar Pipeline</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Conector</label>
              <Select
                value={formData.conectorId ? formData.conectorId.toString() : ''}
                onValueChange={(value) => handleChange('conectorId', parseInt(value))}
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
                onValueChange={(value) => handleChange('pipelineId', parseInt(value))}
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
              <label htmlFor="prioridade" className="text-sm font-medium">Prioridade</label>
              <Input
                id="prioridade"
                type="number"
                value={formData.prioridade}
                onChange={(e) => handleChange('prioridade', parseInt(e.target.value) || 0)}
                min={0}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="agendamento" className="text-sm font-medium">Agendamento</label>
              <Input
                id="agendamento"
                type="datetime-local"
                value={formData.agendamento || ''}
                onChange={(e) => handleChange('agendamento', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="payload" className="text-sm font-medium">Payload (JSON)</label>
              <textarea
                id="payload"
                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={formData.payload || ''}
                onChange={(e) => handleChange('payload', e.target.value)}
                rows={3}
              />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Enfileirando...' : 'Enfileirar'}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  );
}
