import { useState, useEffect } from 'react';
import { PageLayout, DataTable, ConfirmModal, Badge, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { categoriaIntegracaoService } from '../../services/categoriaIntegracaoService';
import type { CategoriaIntegracao } from '../../types/categoriaIntegracao';
import CategoriaIntegracaoFormModal from '../../components/modals/CategoriaIntegracaoFormModal';

export default function CategoriasIntegracao() {
  const { t } = useI18n();
  const [categorias, setCategorias] = useState<CategoriaIntegracao[]>([]);
  const [selectedCategorias, setSelectedCategorias] = useState<CategoriaIntegracao[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingCategoria, setEditingCategoria] = useState<CategoriaIntegracao | null>(null);

  const { execute: fetchCategorias, loading } = useApi<PaginatedResult<CategoriaIntegracao>>({
    showErrorMessage: true,
  });

  const { execute: deleteCategorias } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('integration.category.list.removed'), variant: 'success' });
    },
  });

  const loadCategorias = async () => {
    const result = await fetchCategorias(() => categoriaIntegracaoService.getAll());
    if (result) {
      setCategorias(result.data);
    }
  };

  useEffect(() => {
    loadCategorias();
  }, []);

  const handleAdd = () => {
    setEditingCategoria(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selectedCategorias.length === 1) {
      setEditingCategoria(selectedCategorias[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const categoria of selectedCategorias) {
      await deleteCategorias(() => categoriaIntegracaoService.delete(categoria.id));
    }
    setIsConfirmOpen(false);
    setSelectedCategorias([]);
    loadCategorias();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingCategoria(null);
    setSelectedCategorias([]);
    loadCategorias();
  };

  const columns: DataTableColumn<CategoriaIntegracao>[] = [
    { key: 'nome', title: t('common.column.name'), dataIndex: 'nome' },
    { key: 'descricao', title: t('common.column.description'), dataIndex: 'descricao' },
    {
      key: 'ativo',
      title: t('common.column.status'),
      dataIndex: 'ativo',
      width: 120,
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'secondary'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('integration.category.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadCategorias}
      selectedRowsCount={selectedCategorias.length}
    >
      <div className="space-y-4">
        <DataTable
          columns={columns}
          data={categorias}
          rowKey="id"
          selectedRows={selectedCategorias}
          onSelectionChange={setSelectedCategorias}
          emptyText={t('integration.category.list.empty')}
          loading={loading}
        />
      </div>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('integration.category.list.deleteTitle')}
        description={t('integration.category.list.deleteDescription').replace('{0}', String(selectedCategorias.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <CategoriaIntegracaoFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        categoria={editingCategoria}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
