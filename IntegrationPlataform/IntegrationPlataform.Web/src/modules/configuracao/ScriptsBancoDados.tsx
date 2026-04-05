import { useState, useEffect } from 'react';
import { PageLayout, DataTable, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { scriptBancoDadosService } from '../../services/scriptBancoDadosService';
import type { ScriptBancoDados } from '../../types/scriptBancoDados';
import type { ConexaoBancoDados } from '../../types/conexaoBancoDados';
import ScriptBancoDadosFormModal from '../../components/modals/ScriptBancoDadosFormModal';

export default function ScriptsBancoDados() {
  const { t } = useI18n();
  const [scripts, setScripts] = useState<ScriptBancoDados[]>([]);
  const [selectedScripts, setSelectedScripts] = useState<ScriptBancoDados[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingScript, setEditingScript] = useState<ScriptBancoDados | null>(null);

  const { execute: fetchScripts, loading } = useApi<PaginatedResult<ScriptBancoDados>>({
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
    const result = await fetchScripts(() => scriptBancoDadosService.getAll());
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
      await deleteScripts(() => scriptBancoDadosService.delete(script.id));
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

  const columns: DataTableColumn<ScriptBancoDados>[] = [
    { key: 'nome', title: t('common.column.name'), dataIndex: 'nome' },
    {
      key: 'conexaoBancoDados',
      title: t('common.column.connection'),
      dataIndex: 'conexaoBancoDados',
      render: (value: ConexaoBancoDados) => value?.nome || '-',
    },
    { key: 'descricao', title: t('common.column.description'), dataIndex: 'descricao' },
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
