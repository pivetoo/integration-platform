import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, toast } from 'd-rts';
import { scriptBancoDadosService } from '../../services/scriptBancoDadosService';
import { conexaoBancoDadosService } from '../../services/conexaoBancoDadosService';
import type { ScriptBancoDados, CreateScriptBancoDadosRequest } from '../../types/scriptBancoDados';
import type { ConexaoBancoDados } from '../../types/conexaoBancoDados';

interface ScriptBancoDadosFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  scriptBancoDados: ScriptBancoDados | null;
  onSuccess: () => void;
}

const initialFormData: CreateScriptBancoDadosRequest = {
  conexaoBancoDadosId: 0,
  nome: '',
  descricao: '',
  script: '',
};

export default function ScriptBancoDadosFormModal({ open, onOpenChange, scriptBancoDados, onSuccess }: ScriptBancoDadosFormModalProps) {
  const isEditing = !!scriptBancoDados;
  const [formData, setFormData] = useState<CreateScriptBancoDadosRequest>(initialFormData);
  const [conexoes, setConexoes] = useState<ConexaoBancoDados[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
  });

  const { execute: fetchConexoes } = useApi<ConexaoBancoDados[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open) {
      loadConexoes();
    }
  }, [open]);

  useEffect(() => {
    if (scriptBancoDados) {
      setFormData({
        conexaoBancoDadosId: scriptBancoDados.conexaoBancoDados?.id || 0,
        nome: scriptBancoDados.nome,
        descricao: scriptBancoDados.descricao || '',
        script: scriptBancoDados.script,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [scriptBancoDados]);

  const loadConexoes = async () => {
    const result = await fetchConexoes(async () => {
      const res = await conexaoBancoDadosService.getAll({ pageSize: 1000 });
      return res.data;
    });
    if (result) {
      setConexoes(result);
    }
  };

  const handleChange = (field: keyof CreateScriptBancoDadosRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      let result: { message?: string } | null = null;

      if (isEditing) {
        result = await execute(() => scriptBancoDadosService.update(scriptBancoDados.id, formData));
      } else {
        result = await execute(() => scriptBancoDadosService.create(formData));
      }

      if (result) {
        toast({
          title: 'Sucesso',
          description: result.message || 'Script salvo com sucesso',
          variant: 'success',
        });
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
          <ModalTitle>{isEditing ? 'Editar Script' : 'Novo Script'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="nome" className="text-sm font-medium">Nome</label>
              <Input
                id="nome"
                value={formData.nome}
                onChange={(e) => handleChange('nome', e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Conexão Banco de Dados</label>
              <Select
                value={formData.conexaoBancoDadosId ? formData.conexaoBancoDadosId.toString() : ''}
                onValueChange={(value) => handleChange('conexaoBancoDadosId', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione uma conexão" />
                </SelectTrigger>
                <SelectContent>
                  {conexoes.map((conexao) => (
                    <SelectItem key={conexao.id} value={conexao.id.toString()}>
                      {conexao.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="descricao" className="text-sm font-medium">Descrição</label>
              <Input
                id="descricao"
                value={formData.descricao || ''}
                onChange={(e) => handleChange('descricao', e.target.value)}
              />
            </div>

            <div className="space-y-2 col-span-2">
              <label htmlFor="script" className="text-sm font-medium">Script SQL</label>
              <textarea
                id="script"
                className="flex min-h-[200px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={formData.script}
                onChange={(e) => handleChange('script', e.target.value)}
                required
                rows={8}
                placeholder="SELECT * FROM tabela WHERE ..."
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
