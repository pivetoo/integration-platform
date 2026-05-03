import { useState, useEffect } from 'react';
import { PageLayout, DataTable, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { databaseScriptService } from '../../../services/databaseScriptService';
import type { DatabaseScript } from '../../../types/databaseScript';
import type { DatabaseConnection } from '../../../types/databaseConnection';
import ScriptBancoDadosFormModal from '../../../components/modals/DatabaseScriptFormModal';

export default function ScriptsBancoDados() {
  const { t } = useI18n();
  const [scripts, setScripts] = useState<DatabaseScript[]>([]);
  const [selectedScripts, setSelectedScripts] = useState<DatabaseScript[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingScript, setEditingScript] = useState<DatabaseScript | null>(null);

  const { execute: fetchScripts, loading } = useApi<PaginatedResult<DatabaseScript>>({
    showErrorMessage: true,
  });

  const { execute: deleteScripts } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('database.script.list.removed'), variant: 'success' });
    },
  });

  const loadScripts = async () => {
    const result = await fetchScripts(() => databaseScriptService.getAll());
    if (result) {
      setScripts(result.data);
    }
  };

  useEffect(() => {
    loadScripts();
  }, []);

  const handleAdd = () => {
    setEditingScript(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selectedScripts.length === 1) {
      setEditingScript(selectedScripts[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const script of selectedScripts) {
      await deleteScripts(() => databaseScriptService.delete(script.id));
    }
    setIsConfirmOpen(false);
    setSelectedScripts([]);
    loadScripts();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingScript(null);
    setSelectedScripts([]);
    loadScripts();
  };

  const columns: DataTableColumn<DatabaseScript>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name' },
    {
      key: 'databaseConnection',
      title: t('common.column.connection'),
      dataIndex: 'databaseConnection',
      render: (value: DatabaseConnection) => value?.name || '-',
    },
    { key: 'description', title: t('common.column.description'), dataIndex: 'description' },
  ];

  return (
    <PageLayout
      title={t('database.script.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadScripts}
      selectedRowsCount={selectedScripts.length}
    >
      <DataTable
        columns={columns}
        data={scripts}
        rowKey="id"
        selectedRows={selectedScripts}
        onSelectionChange={setSelectedScripts}
        emptyText={t('database.script.list.empty')}
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('database.script.list.deleteTitle')}
        description={t('database.script.list.deleteDescription').replace('{0}', String(selectedScripts.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <ScriptBancoDadosFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        scriptBancoDados={editingScript}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
