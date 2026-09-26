import { useEffect, useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, ConfirmModal, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { databaseScriptService } from '../../../services/databaseScriptService';
import type { DatabaseScript } from '../../../types/databaseScript';
import type { DatabaseConnection } from '../../../types/databaseConnection';
import ScriptBancoDadosFormModal from '../../../components/modals/DatabaseScriptFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';

export default function ScriptsBancoDados() {
  const { t } = useI18n();
  const [scripts, setScripts] = useState<DatabaseScript[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRows, setSelectedRows] = useState<DatabaseScript[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<DatabaseScript[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingScript, setEditingScript] = useState<DatabaseScript | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchScripts, loading } = useApi<PaginatedResult<DatabaseScript>>({
    showErrorMessage: true,
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

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (script) => databaseScriptService.delete(script.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadScripts();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingScript(null);
    setSelectedRows([]);
    void loadScripts();
  };

  const rowActions: DataTableRowAction<DatabaseScript>[] = [
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingScript(row);
        setIsFormOpen(true);
      },
    },
    {
      key: 'delete',
      label: t('common.action.delete'),
      icon: <Trash2 className="h-4 w-4" />,
      variant: 'danger',
      onClick: (row) => {
        setItemsToDelete([row]);
        setIsConfirmOpen(true);
      },
    },
  ];

  const bulkActions: DataTableBulkAction<DatabaseScript>[] = [
    {
      key: 'delete',
      label: t('common.action.delete'),
      icon: <Trash2 className="h-4 w-4" />,
      variant: 'danger',
      disabled: bulkRunning,
      onClick: (rows) => {
        setItemsToDelete(rows);
        setIsConfirmOpen(true);
      },
    },
  ];

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
      onRefresh={() => void loadScripts()}
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
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        rowActions={rowActions}
        bulkActions={bulkActions}
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
        description={t('database.script.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
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
