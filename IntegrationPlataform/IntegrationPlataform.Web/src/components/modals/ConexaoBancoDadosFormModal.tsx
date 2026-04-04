import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, toast } from 'd-rts';
import { conexaoBancoDadosService } from '../../services/conexaoBancoDadosService';
import type { CreateConexaoBancoDadosRequest } from '../../types/conexaoBancoDados';
import { TipoBancoDados, TipoBancoDadosLabels } from '../../types/conexaoBancoDados';
import type { ConexaoBancoDados } from '../../types/conexaoBancoDados';

interface ConexaoBancoDadosFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
  conexao: ConexaoBancoDados | null;
}

const initialFormData: CreateConexaoBancoDadosRequest = {
  nome: '',
  tipo: TipoBancoDados.PostgreSQL,
  host: '',
  port: 5432,
  database: '',
  username: '',
  password: '',
};

export default function ConexaoBancoDadosFormModal({ open, onOpenChange, onSuccess, conexao }: ConexaoBancoDadosFormModalProps) {
  const isEditing = !!conexao;
  const [formData, setFormData] = useState<CreateConexaoBancoDadosRequest>(initialFormData);

  const { execute, loading } = useApi({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const { execute: testarConexao, loading: testando } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
  });

  useEffect(() => {
    if (conexao) {
      setFormData({
        nome: conexao.nome,
        tipo: conexao.tipo,
        host: conexao.host,
        port: conexao.port,
        database: conexao.database,
        username: conexao.username,
        password: conexao.password,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [conexao]);

  const handleChange = (field: keyof CreateConexaoBancoDadosRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleTestar = async () => {
    if (!formData.host || !formData.database || !formData.username || !formData.password) {
      toast({ title: 'Preencha todos os campos para testar a conexão', variant: 'destructive' });
      return;
    }
    const result = await testarConexao(() => conexaoBancoDadosService.testar(formData));
    if (result) {
      toast({
        title: 'Sucesso',
        description: result.message || 'Conexao testada com sucesso',
        variant: 'success',
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEditing) {
        await execute(() => conexaoBancoDadosService.update(conexao.id, formData));
      } else {
        await execute(() => conexaoBancoDadosService.create(formData));
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
          <ModalTitle>{isEditing ? 'Editar Conexão' : 'Nova Conexão de Banco'}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="nome" className="text-sm font-medium">Nome</label>
              <Input
                id="nome"
                value={formData.nome}
                onChange={(e) => handleChange('nome', e.target.value)}
                placeholder="Conexão Protheus"
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo de Banco</label>
              <Select
                value={formData.tipo.toString()}
                onValueChange={(value) => handleChange('tipo', parseInt(value))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(TipoBancoDadosLabels).map(([key, label]) => (
                    <SelectItem key={key} value={key}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label htmlFor="host" className="text-sm font-medium">Host</label>
              <Input
                id="host"
                value={formData.host}
                onChange={(e) => handleChange('host', e.target.value)}
                placeholder="localhost ou 192.168.1.100"
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="port" className="text-sm font-medium">Porta</label>
              <Input
                id="port"
                type="number"
                value={formData.port}
                onChange={(e) => handleChange('port', parseInt(e.target.value))}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="database" className="text-sm font-medium">Database</label>
              <Input
                id="database"
                value={formData.database}
                onChange={(e) => handleChange('database', e.target.value)}
                placeholder="nome_do_banco"
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="username" className="text-sm font-medium">Username</label>
              <Input
                id="username"
                value={formData.username}
                onChange={(e) => handleChange('username', e.target.value)}
                placeholder="postgres"
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="password" className="text-sm font-medium">Password</label>
              <Input
                id="password"
                type="password"
                value={formData.password}
                onChange={(e) => handleChange('password', e.target.value)}
                required
              />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="button" variant="secondary" onClick={handleTestar} disabled={testando}>
              {testando ? 'Testando...' : 'Testar Conexão'}
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
