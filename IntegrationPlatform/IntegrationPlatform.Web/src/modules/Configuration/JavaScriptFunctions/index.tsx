import { useEffect, useState } from 'react';
import { PageLayout, DataTable, ConfirmModal, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { javaScriptFunctionService } from '../../../services/javaScriptFunctionService';
import type { JavaScriptFunction } from '../../../types/javaScriptFunction';
import FuncaoJavaScriptFormModal from '../../../components/modals/JavaScriptFunctionFormModal';

export default function FuncoesJavaScript() {
  const { t } = useI18n();
  const [funcoes, setFuncoes] = useState<JavaScriptFunction[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedFuncoes, setSelectedFuncoes] = useState<JavaScriptFunction[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingFuncao, setEditingFuncao] = useState<JavaScriptFunction | null>(null);

  const { execute: fetchFuncoes, loading } = useApi<PaginatedResult<JavaScriptFunction>>({
    showErrorMessage: true,
  });

  const { execute: deleteFuncoes } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('javaScriptFunction.list.removed'), variant: 'success' });
    },
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

  const handleEdit = () => {
    if (selectedFuncoes.length === 1) {
      setEditingFuncao(selectedFuncoes[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const funcao of selectedFuncoes) {
      await deleteFuncoes(() => javaScriptFunctionService.delete(funcao.id));
    }
    setIsConfirmOpen(false);
    setSelectedFuncoes([]);
    void loadFuncoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingFuncao(null);
    setSelectedFuncoes([]);
    void loadFuncoes();
  };

  const columns: DataTableColumn<JavaScriptFunction>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name', sortable: true },
    { key: 'description', title: t('common.column.description'), dataIndex: 'description', hiddenBelow: 'md' },
  ];

  return (
    <PageLayout
      title={t('javaScriptFunction.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadFuncoes()}
      selectedRowsCount={selectedFuncoes.length}
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
        selectable
        selectedRows={selectedFuncoes}
        onSelectionChange={setSelectedFuncoes}
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
        description={t('javaScriptFunction.list.deleteDescription').replace('{0}', String(selectedFuncoes.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
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
