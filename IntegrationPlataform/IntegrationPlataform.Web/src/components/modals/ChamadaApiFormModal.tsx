import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'd-rts';
import { chamadaApiService } from '../../services/chamadaApiService';
import { MetodoHttp, MetodoHttpLabels } from '../../types/chamadaApi';
import type { ChamadaApi, CreateChamadaApiRequest } from '../../types/chamadaApi';

interface ChamadaApiFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  chamadaApi: ChamadaApi | null;
  onSuccess: () => void;
}

const initialFormData: CreateChamadaApiRequest = {
  nome: '',
  descricao: '',
  metodo: MetodoHttp.GET,
  url: '',
  headersTemplate: '',
  bodyTemplate: '',
};

export default function ChamadaApiFormModal({ open, onOpenChange, chamadaApi, onSuccess }: ChamadaApiFormModalProps) {
  const isEditing = !!chamadaApi;
  const [formData, setFormData] = useState<CreateChamadaApiRequest>(initialFormData);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (chamadaApi) {
      setFormData({
        nome: chamadaApi.nome,
        descricao: chamadaApi.descricao || '',
        metodo: chamadaApi.metodo,
        url: chamadaApi.url,
        headersTemplate: chamadaApi.headersTemplate || '',
        bodyTemplate: chamadaApi.bodyTemplate || '',
      });
    } else {
      setFormData(initialFormData);
    }
  }, [chamadaApi]);

  const handleChange = (field: keyof CreateChamadaApiRequest, value: string | number) => {
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

  const metodoOptions = Object.entries(MetodoHttpLabels).map(([value, label]) => ({ value, label }));

  const showBody = formData.metodo !== MetodoHttp.GET && formData.metodo !== MetodoHttp.DELETE;

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="3xl">
        <ModalHeader>
          <ModalTitle>{isEditing ? 'Editar Chamada de API' : 'Nova Chamada de API'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2 col-span-2">
              <label htmlFor="nome" className="text-sm font-medium">Nome</label>
              <Input
                id="nome"
                value={formData.nome}
                onChange={(e) => handleChange('nome', e.target.value)}
                placeholder=""
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
              <label className="text-sm font-medium">Método</label>
              <Select
                value={formData.metodo.toString()}
                onValueChange={(value) => handleChange('metodo', parseInt(value))}
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
              <label htmlFor="url" className="text-sm font-medium">URL</label>
              <Input
                id="url"
                value={formData.url}
                onChange={(e) => handleChange('url', e.target.value)}
                placeholder="/api/produtos"
                required
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="headersTemplate" className="text-sm font-medium">Headers (JSON Template)</label>
              <textarea
                id="headersTemplate"
                className="flex min-h-[60px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={formData.headersTemplate || ''}
                onChange={(e) => handleChange('headersTemplate', e.target.value)}
                placeholder='{"Content-Type": "application/json"}'
                rows={2}
              />
            </div>

            {showBody && (
              <div className="space-y-2 col-span-2">
                <label htmlFor="bodyTemplate" className="text-sm font-medium">Body (JSON Template)</label>
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
