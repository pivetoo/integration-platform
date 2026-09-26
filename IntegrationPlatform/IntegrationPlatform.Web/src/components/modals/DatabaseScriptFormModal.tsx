import { useEffect, useState } from 'react';
import { Modal, ModalBody, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi, useI18n, toast } from 'archon-ui';
import { databaseScriptService } from '../../services/databaseScriptService';
import { databaseConnectionService } from '../../services/databaseConnectionService';
import type { DatabaseScript, CreateDatabaseScriptRequest } from '../../types/databaseScript';
import type { DatabaseConnection } from '../../types/databaseConnection';

interface DatabaseScriptFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  scriptBancoDados: DatabaseScript | null;
  onSuccess: () => void;
}

const initialFormData: CreateDatabaseScriptRequest = {
  databaseConnectionId: 0,
  name: '',
  description: '',
  script: '',
};

export default function DatabaseScriptFormModal({ open, onOpenChange, scriptBancoDados, onSuccess }: DatabaseScriptFormModalProps) {
  const { t } = useI18n();
  const isEditing = !!scriptBancoDados;
  const [formData, setFormData] = useState<CreateDatabaseScriptRequest>(initialFormData);
  const [conexoes, setConexoes] = useState<DatabaseConnection[]>([]);

  const { execute, loading } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
  });

  const { execute: fetchConexoes } = useApi<DatabaseConnection[]>({
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
        databaseConnectionId: scriptBancoDados.databaseConnectionId || scriptBancoDados.databaseConnection?.id || 0,
        name: scriptBancoDados.name,
        description: scriptBancoDados.description || '',
        script: scriptBancoDados.script,
      });
    } else {
      setFormData(initialFormData);
    }
  }, [scriptBancoDados]);

  const loadConexoes = async () => {
    const result = await fetchConexoes(async () => {
      const res = await databaseConnectionService.getAll({ pageSize: 1000 });
      return res.data;
    });
    if (result) {
      setConexoes(result);
    }
  };

  const handleChange = (field: keyof CreateDatabaseScriptRequest, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const result = await execute(() =>
      isEditing
        ? databaseScriptService.update(scriptBancoDados.id, formData)
        : databaseScriptService.create(formData)
    );

    if (result !== null) {
      toast({
        title: t('common.toast.successTitle'),
        description: t('database.script.form.saved'),
        variant: 'success',
      });
      onSuccess();
    }
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="3xl">
        <ModalHeader>
          <ModalTitle>{isEditing ? t('database.script.form.editTitle') : t('database.script.form.createTitle')}</ModalTitle>
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
                  required
                />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium">{t('common.column.connection')}</label>
                <Select
                  value={formData.databaseConnectionId ? formData.databaseConnectionId.toString() : ''}
                  onValueChange={(value) => handleChange('databaseConnectionId', parseInt(value))}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('database.script.form.connectionPlaceholder')} />
                  </SelectTrigger>
                  <SelectContent>
                    {conexoes.map((conexao) => (
                      <SelectItem key={conexao.id} value={conexao.id.toString()}>
                        {conexao.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2 col-span-2">
                <label htmlFor="description" className="text-sm font-medium">{t('common.column.description')}</label>
                <Input
                  id="description"
                  value={formData.description || ''}
                  onChange={(e) => handleChange('description', e.target.value)}
                />
              </div>

              <div className="space-y-2 col-span-2">
                <label htmlFor="script" className="text-sm font-medium">{t('database.script.form.scriptLabel')}</label>
                <textarea
                  id="script"
                  className="flex min-h-[200px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm font-mono ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  value={formData.script}
                  onChange={(e) => handleChange('script', e.target.value)}
                  required
                  rows={8}
                  placeholder={t('database.script.form.scriptPlaceholder')}
                />
              </div>
            </div>
          </ModalBody>

          <ModalFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              {t('common.action.cancel')}
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
