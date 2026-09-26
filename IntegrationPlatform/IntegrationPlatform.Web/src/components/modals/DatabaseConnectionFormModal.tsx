import { useEffect, useState } from 'react';
import { Modal, ModalBody, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n, toast } from 'archon-ui';
import { databaseConnectionService } from '../../services/databaseConnectionService';
import type { CreateDatabaseConnectionRequest } from '../../types/databaseConnection';
import { DatabaseType, DatabaseTypeLabels } from '../../types/databaseConnection';
import type { DatabaseConnection } from '../../types/databaseConnection';

interface DatabaseConnectionFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
  conexao: DatabaseConnection | null;
}

const initialFormData: CreateDatabaseConnectionRequest = {
  name: '',
  type: DatabaseType.PostgreSQL,
  host: '',
  port: 5432,
  database: '',
  username: '',
  password: '',
};

export default function DatabaseConnectionFormModal({ open, onOpenChange, onSuccess, conexao }: DatabaseConnectionFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!conexao;
  const [formData, setFormData] = useState<CreateDatabaseConnectionRequest>(initialFormData);

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
        name: conexao.name,
        type: conexao.type,
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

  const handleChange = (field: keyof CreateDatabaseConnectionRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleTestar = async () => {
    if (!formData.host || !formData.database || !formData.username || !formData.password) {
      toast({ title: t('database.connection.form.testMissingFields'), variant: 'destructive' });
      return;
    }
    const result = await testarConexao(() => databaseConnectionService.test(formData));
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
    const result = await execute(() =>
      isEditing
        ? databaseConnectionService.update(conexao.id, formData)
        : databaseConnectionService.create(formData)
    );

    if (result !== null) {
      onSuccess();
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="3xl">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('database.connection.form.editTitle') : t('database.connection.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col gap-3 sm:gap-4">
          <ModalBody className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="name" className="text-sm font-medium">{t('common.column.name')}</label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => handleChange('name', e.target.value)}
                  placeholder={t('database.connection.form.namePlaceholder')}
                  required
                />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">{t('database.connection.form.databaseType')}</label>
                <Select
                  value={formData.type.toString()}
                  onValueChange={(value) => handleChange('type', parseInt(value))}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(DatabaseTypeLabels).map(([key, label]) => (
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
          </ModalBody>

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
