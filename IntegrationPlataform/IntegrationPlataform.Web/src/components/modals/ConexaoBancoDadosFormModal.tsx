import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n, toast } from 'archon-ui';
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
  const { t } = useI18n();
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
      toast({ title: t('database.connection.form.testMissingFields'), variant: 'destructive' });
      return;
    }
    const result = await testarConexao(() => conexaoBancoDadosService.testar(formData));
    if (result) {
      toast({
        title: 'Sucesso',
        description: result.message || t('database.connection.form.testSuccess'),
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
          <ModalTitle>{isEditing ? t('database.connection.form.editTitle') : t('database.connection.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="nome" className="text-sm font-medium">{t('common.column.name')}</label>
              <Input
                id="nome"
                value={formData.nome}
                onChange={(e) => handleChange('nome', e.target.value)}
                placeholder={t('database.connection.form.namePlaceholder')}
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('database.connection.form.databaseType')}</label>
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
              <label htmlFor="host" className="text-sm font-medium">{t('common.column.host')}</label>
              <Input
                id="host"
                value={formData.host}
                onChange={(e) => handleChange('host', e.target.value)}
                placeholder={t('database.connection.form.hostPlaceholder')}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="port" className="text-sm font-medium">{t('database.connection.form.port')}</label>
              <Input
                id="port"
                type="number"
                value={formData.port}
                onChange={(e) => handleChange('port', parseInt(e.target.value))}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="database" className="text-sm font-medium">{t('common.column.database')}</label>
              <Input
                id="database"
                value={formData.database}
                onChange={(e) => handleChange('database', e.target.value)}
                placeholder={t('database.connection.form.databasePlaceholder')}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="username" className="text-sm font-medium">{t('common.column.username')}</label>
              <Input
                id="username"
                value={formData.username}
                onChange={(e) => handleChange('username', e.target.value)}
                placeholder={t('database.connection.form.usernamePlaceholder')}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="password" className="text-sm font-medium">{t('database.connection.form.password')}</label>
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
              {t('common.action.cancel')}
            </Button>
            <Button type="button" variant="secondary" onClick={handleTestar} disabled={testando}>
              {testando ? t('common.action.testing') : t('database.connection.form.testAction')}
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? t('common.action.saving') : t('common.action.save')}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  );
}
