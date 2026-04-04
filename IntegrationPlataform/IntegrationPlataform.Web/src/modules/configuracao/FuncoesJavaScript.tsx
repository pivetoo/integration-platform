import { useState, useEffect } from 'react';
import { PageLayout, DataTable, ConfirmModal, useApi, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { funcaoJavaScriptService } from '../../services/funcaoJavaScriptService';
import type { FuncaoJavaScript } from '../../types/funcaoJavaScript';
import FuncaoJavaScriptFormModal from '../../components/modals/FuncaoJavaScriptFormModal';

export default function FuncoesJavaScript() {
  const [funcoes, setFuncoes] = useState<FuncaoJavaScript[]>([]);
  const [selectedFuncoes, setSelectedFuncoes] = useState<FuncaoJavaScript[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingFuncao, setEditingFuncao] = useState<FuncaoJavaScript | null>(null);

  const { execute: fetchFuncoes, loading } = useApi<PaginatedResult<FuncaoJavaScript>>({
    showErrorMessage: true,
  });

  const { execute: deleteFuncoes } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: 'Removido', description: 'Função JavaScript removida com sucesso', variant: 'success' });
    },
  });

  const loadFuncoes = async () => {
    const result = await fetchFuncoes(() => funcaoJavaScriptService.getAll());
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
      await deleteFuncoes(() => funcaoJavaScriptService.delete(funcao.id));
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

  const columns: DataTableColumn<FuncaoJavaScript>[] = [
    { key: 'nome', title: 'Nome', dataIndex: 'nome' },
    { key: 'descricao', title: 'Descrição', dataIndex: 'descricao' },
  ];

  return (
    <PageLayout
      title="Funções JavaScript"
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
        emptyText="Nenhuma função JavaScript encontrada"
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Excluir Função JavaScript"
        description={`Tem certeza que deseja excluir ${selectedFuncoes.length} função(ões)?`}
        confirmText="Excluir"
        cancelText="Cancelar"
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
