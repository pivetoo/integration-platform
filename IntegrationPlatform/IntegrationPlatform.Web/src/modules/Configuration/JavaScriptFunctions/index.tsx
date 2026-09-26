import { useEffect, useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, ConfirmModal, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { javaScriptFunctionService } from '../../../services/javaScriptFunctionService';
import type { JavaScriptFunction } from '../../../types/javaScriptFunction';
import FuncaoJavaScriptFormModal from '../../../components/modals/JavaScriptFunctionFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';

export default function FuncoesJavaScript() {
  const { t } = useI18n();
  const [funcoes, setFuncoes] = useState<JavaScriptFunction[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRows, setSelectedRows] = useState<JavaScriptFunction[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<JavaScriptFunction[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingFuncao, setEditingFuncao] = useState<JavaScriptFunction | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchFuncoes, loading } = useApi<PaginatedResult<JavaScriptFunction>>({
    showErrorMessage: true,
  });

  const loadFuncoes = async () => {
    const result = await fetchFuncoes(() =>
      javaScriptFunctionService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      setFuncoes(result.data);
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
    void loadFuncoes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch]);

  const handleAdd = () => {
    setEditingFuncao(null);
    setIsFormOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (funcao) => javaScriptFunctionService.delete(funcao.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadFuncoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingFuncao(null);
    setSelectedRows([]);
    void loadFuncoes();
  };

  const rowActions: DataTableRowAction<JavaScriptFunction>[] = [
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingFuncao(row);
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

  const bulkActions: DataTableBulkAction<JavaScriptFunction>[] = [
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

  const columns: DataTableColumn<JavaScriptFunction>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name', sortable: true },
    { key: 'description', title: t('common.column.description'), dataIndex: 'description', hiddenBelow: 'md' },
  ];

  return (
    <PageLayout
      title={t('javaScriptFunction.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadFuncoes()}
    >
      <TableToolbar
        searchValue={search}
        onSearchChange={setSearch}
        searchPlaceholder={t('common.action.search')}
        className="mb-3"
      />

      <DataTable
        columns={columns}
        data={funcoes}
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
        title={t('javaScriptFunction.list.deleteTitle')}
        description={t('javaScriptFunction.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
      />

      <FuncaoJavaScriptFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        funcaoJavaScript={editingFuncao}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
