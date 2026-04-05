import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { apiCallService } from '../../../services/apiCallService';
import { HttpMethodLabels } from '../../../types/apiCall';
import type { ApiCall, HttpMethod } from '../../../types/apiCall';
import ChamadaApiFormModal from '../../../components/modals/ApiCallFormModal';

const metodoVariantMap: Record<number, string> = {
  0: 'success',
  1: 'default',
  2: 'warning',
  3: 'secondary',
  4: 'destructive',
};

export default function ChamadasApi() {
  const { t } = useI18n();
  const [chamadas, setChamadas] = useState<ApiCall[]>([]);
  const [selectedChamadas, setSelectedChamadas] = useState<ApiCall[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingChamada, setEditingChamada] = useState<ApiCall | null>(null);

  const { execute: fetchChamadas, loading } = useApi<PaginatedResult<ApiCall>>({
    showErrorMessage: true,
  });
  const { execute: fetchApiCallById } = useApi<ApiCall>({
    showErrorMessage: false,
  });

  const { execute: deleteChamadas } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('apiCall.list.removed'), variant: 'success' });
    },
  });

  const loadChamadas = async () => {
    const result = await fetchChamadas(() => apiCallService.getAll());
    if (result) {
      setChamadas(result.data);
    }
  };

  useEffect(() => {
    loadChamadas();
  }, []);

  const handleAdd = () => {
    setEditingChamada(null);
    setIsFormOpen(true);
  };

  const handleEdit = async () => {
    if (selectedChamadas.length === 1) {
      const result = await fetchApiCallById(() => apiCallService.getById(selectedChamadas[0].id));
      setEditingChamada(result ?? selectedChamadas[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const chamada of selectedChamadas) {
      await deleteChamadas(() => apiCallService.delete(chamada.id));
    }
    setIsConfirmOpen(false);
    setSelectedChamadas([]);
    loadChamadas();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingChamada(null);
    setSelectedChamadas([]);
    loadChamadas();
  };

  const columns: DataTableColumn<ApiCall>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name' },
    {
      key: 'method',
      title: t('common.column.method'),
      dataIndex: 'method',
      render: (value: HttpMethod) => (
        <Badge variant={(metodoVariantMap[value] || 'outline') as 'success' | 'default' | 'warning' | 'secondary' | 'destructive'}>
          {HttpMethodLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'url', title: t('common.column.url'), dataIndex: 'url' },
  ];

  return (
    <PageLayout
      title={t('apiCall.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadChamadas}
      selectedRowsCount={selectedChamadas.length}
    >
      <DataTable
        columns={columns}
        data={chamadas}
        rowKey="id"
        selectedRows={selectedChamadas}
        onSelectionChange={setSelectedChamadas}
        emptyText={t('apiCall.list.empty')}
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('apiCall.list.deleteTitle')}
        description={t('apiCall.list.deleteDescription').replace('{0}', String(selectedChamadas.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <ChamadaApiFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        chamadaApi={editingChamada}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
