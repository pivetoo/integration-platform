import { useState, useEffect } from 'react';
import { PageLayout, DataTable, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { referenceService } from '../../../services/referenceService';
import type { Reference } from '../../../types/reference';
import type { Conector } from '../../../types/connector';
import ReferenceFormModal from '../../../components/modals/ReferenceFormModal';
import { formatDateTime } from '../../../utils/formatters';

export default function References() {
  const { t } = useI18n();
  const [referencias, setReferencias] = useState<Reference[]>([]);
  const [selectedReferencias, setSelectedReferencias] = useState<Reference[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingReferencia, setEditingReferencia] = useState<Reference | null>(null);

  const { execute: fetchReferencias, loading } = useApi<PaginatedResult<Reference>>({
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
    const result = await fetchReferencias(() => referenceService.getAll());
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
      await deleteReferencias(() => referenceService.delete(referencia.id));
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

  const columns: DataTableColumn<Reference>[] = [
    {
      key: 'connector',
      title: t('common.column.connector'),
      dataIndex: 'connector',
      render: (value: Conector) => value?.name || '-',
    },
    {
      key: 'entity',
      title: t('common.column.entity'),
      dataIndex: 'entity',
    },
    {
      key: 'internalId',
      title: t('common.column.internalId'),
      dataIndex: 'internalId',
    },
    {
      key: 'externalId',
      title: t('common.column.externalId'),
      dataIndex: 'externalId',
    },
    {
      key: 'createdAt',
      title: t('common.column.createdAt'),
      dataIndex: 'createdAt',
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

      <ReferenceFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        referencia={editingReferencia}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
