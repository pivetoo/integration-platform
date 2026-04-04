import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'd-rts';
import { referenciaService } from '../../services/referenciaService';
import { conectorService } from '../../services/conectorService';
import type { Referencia, CreateReferenciaRequest } from '../../types/referencia';
import type { Conector } from '../../types/conector';

interface ReferenciaFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  referencia: Referencia | null;
  onSuccess: () => void;
}

const initialFormData: CreateReferenciaRequest = {
  conectorId: 0,
  entidade: '',
  idInterno: '',
  idExterno: '',
};

export default function ReferenciaFormModal({ open, onOpenChange, referencia, onSuccess }: ReferenciaFormModalProps) {
  const isEditing = !!referencia;
  const [formData, setFormData] = useState<CreateReferenciaRequest>(initialFormData);
  const [conectores, setConectores] = useState<Conector[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: fetchConectores } = useApi<Conector[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadConectores();
    }
  }, [open]);

  useEffect(() => {
    if (referencia) {
      setFormData({
        conectorId: referencia.conector?.id || 0,
        entidade: referencia.entidade,
        idInterno: referencia.idInterno,
        idExterno: referencia.idExterno,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [referencia]);

  const loadConectores = async () => {
    const result = await fetchConectores(() => conectorService.getAtivos());
    if (result) {
      setConectores(result);
    }
  };

  const handleChange = (field: keyof CreateReferenciaRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        const { conectorId: _, ...updateData } = formData;
        await execute(() => referenciaService.update(referencia.id, updateData));
      } else {
        await execute(() => referenciaService.create(formData));
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
          <ModalTitle>{isEditing ? 'Editar Referência' : 'Nova Referência'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Conector</label>
              <Select
                value={formData.conectorId ? formData.conectorId.toString() : ''}
                onValueChange={(value) => handleChange('conectorId', parseInt(value))}
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
              <label htmlFor="entidade" className="text-sm font-medium">Entidade</label>
              <Input
                id="entidade"
                value={formData.entidade}
                onChange={(e) => handleChange('entidade', e.target.value)}
                placeholder="Ex: Produto, Pedido, Cliente"
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="idInterno" className="text-sm font-medium">ID Interno</label>
              <Input
                id="idInterno"
                value={formData.idInterno}
                onChange={(e) => handleChange('idInterno', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="idExterno" className="text-sm font-medium">ID Externo</label>
              <Input
                id="idExterno"
                value={formData.idExterno}
                onChange={(e) => handleChange('idExterno', e.target.value)}
                required
              />
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
