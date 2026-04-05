import { useState, useEffect } from 'react';
import { PageLayout, DataTable, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { referenciaService } from '../../services/referenciaService';
import type { Referencia } from '../../types/referencia';
import type { Conector } from '../../types/conector';
import ReferenciaFormModal from '../../components/modals/ReferenciaFormModal';

function formatDateTime(dateStr?: string): string {
  if (!dateStr) {
    return '-';
  }
  return new Date(dateStr).toLocaleString('pt-BR');
}

export default function Referencias() {
  const { t } = useI18n();
  const [referencias, setReferencias] = useState<Referencia[]>([]);
  const [selectedReferencias, setSelectedReferencias] = useState<Referencia[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingReferencia, setEditingReferencia] = useState<Referencia | null>(null);

  const { execute: fetchReferencias, loading } = useApi<PaginatedResult<Referencia>>({
    showErrorMessage: true,
  });

  const { execute: deleteReferencias } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('reference.list.removed'), variant: 'success' });
    },
  });

  const loadReferencias = async () => {
    const result = await fetchReferencias(() => referenciaService.getAll());
    if (result) {
      setReferencias(result.data);
    }
  };

  useEffect(() => {
    loadReferencias();
  }, []);

  const handleAdd = () => {
    setEditingReferencia(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selectedReferencias.length === 1) {
      setEditingReferencia(selectedReferencias[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const referencia of selectedReferencias) {
      await deleteReferencias(() => referenciaService.delete(referencia.id));
    }
    setIsConfirmOpen(false);
    setSelectedReferencias([]);
    loadReferencias();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingReferencia(null);
    setSelectedReferencias([]);
    loadReferencias();
  };

  const columns: DataTableColumn<Referencia>[] = [
    {
      key: 'conector',
      title: t('common.column.connector'),
      dataIndex: 'conector',
      render: (value: Conector) => value?.nome || '-',
    },
    {
      key: 'entidade',
      title: t('common.column.entity'),
      dataIndex: 'entidade',
    },
    {
      key: 'idInterno',
      title: t('common.column.internalId'),
      dataIndex: 'idInterno',
    },
    {
      key: 'idExterno',
      title: t('common.column.externalId'),
      dataIndex: 'idExterno',
    },
    {
      key: 'criadoEm',
      title: t('common.column.createdAt'),
      dataIndex: 'criadoEm',
      render: (value: string) => formatDateTime(value),
    },
  ];

  return (
    <PageLayout
      title={t('reference.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadReferencias}
      selectedRowsCount={selectedReferencias.length}
    >
      <DataTable
        columns={columns}
        data={referencias}
        rowKey="id"
        selectedRows={selectedReferencias}
        onSelectionChange={setSelectedReferencias}
        emptyText={t('reference.list.empty')}
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('reference.list.deleteTitle')}
        description={t('reference.list.deleteDescription').replace('{0}', String(selectedReferencias.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <ReferenciaFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        referencia={editingReferencia}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
