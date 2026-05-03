import { useState, useEffect } from 'react';
import { PageLayout, DataTable, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { javaScriptFunctionService } from '../../../services/javaScriptFunctionService';
import type { JavaScriptFunction } from '../../../types/javaScriptFunction';
import FuncaoJavaScriptFormModal from '../../../components/modals/JavaScriptFunctionFormModal';

export default function FuncoesJavaScript() {
  const { t } = useI18n();
  const [funcoes, setFuncoes] = useState<JavaScriptFunction[]>([]);
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
    const result = await fetchFuncoes(() => javaScriptFunctionService.getAll());
    if (result) {
      setFuncoes(result.data);
    }
  };

  useEffect(() => {
    loadFuncoes();
  }, []);

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
    loadFuncoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingFuncao(null);
    setSelectedFuncoes([]);
    loadFuncoes();
  };

  const columns: DataTableColumn<JavaScriptFunction>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name' },
    { key: 'description', title: t('common.column.description'), dataIndex: 'description' },
  ];

  return (
    <PageLayout
      title={t('javaScriptFunction.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadFuncoes}
      selectedRowsCount={selectedFuncoes.length}
    >
      <DataTable
        columns={columns}
        data={funcoes}
        rowKey="id"
        selectedRows={selectedFuncoes}
        onSelectionChange={setSelectedFuncoes}
        emptyText={t('javaScriptFunction.list.empty')}
        loading={loading}
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
