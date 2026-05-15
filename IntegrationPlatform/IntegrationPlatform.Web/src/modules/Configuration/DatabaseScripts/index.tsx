import { useEffect, useState } from 'react';
import { PageLayout, DataTable, ConfirmModal, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { databaseScriptService } from '../../../services/databaseScriptService';
import type { DatabaseScript } from '../../../types/databaseScript';
import type { DatabaseConnection } from '../../../types/databaseConnection';
import ScriptBancoDadosFormModal from '../../../components/modals/DatabaseScriptFormModal';

export default function ScriptsBancoDados() {
  const { t } = useI18n();
  const [scripts, setScripts] = useState<DatabaseScript[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
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
    const result = await fetchScripts(() =>
      databaseScriptService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      setScripts(result.data);
      setTotalCount(result.total ?? 0);
    }
  };

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timeout);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  useEffect(() => {
    void loadScripts();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch]);

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
    void loadScripts();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingScript(null);
    setSelectedScripts([]);
    void loadScripts();
  };

  const columns: DataTableColumn<DatabaseScript>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name', sortable: true },
    {
      key: 'databaseConnection',
      title: t('common.column.connection'),
      dataIndex: 'databaseConnection',
      hiddenBelow: 'sm',
      render: (value: DatabaseConnection) => value?.name || '-',
    },
    { key: 'description', title: t('common.column.description'), dataIndex: 'description', hiddenBelow: 'md' },
  ];

  return (
    <PageLayout
      title={t('database.script.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadScripts()}
      selectedRowsCount={selectedScripts.length}
    >
      <TableToolbar
        searchValue={search}
        onSearchChange={setSearch}
        searchPlaceholder={t('common.action.search')}
        className="mb-3"
      />

      <DataTable
        columns={columns}
        data={scripts}
        rowKey="id"
        loading={loading}
        selectable
        selectedRows={selectedScripts}
        onSelectionChange={setSelectedScripts}
        emptyText={t('common.state.empty')}
        pageSize={pageSize}
        pageSizeOptions={[10, 20, 50]}
        totalCount={totalCount}
        page={page}
        onPageChange={setPage}
        onPageSizeChange={(s) => {
          setPageSize(s);
          setPage(1);
        }}
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
