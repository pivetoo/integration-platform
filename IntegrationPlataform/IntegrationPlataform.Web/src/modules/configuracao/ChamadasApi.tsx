import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { chamadaApiService } from '../../services/chamadaApiService';
import { MetodoHttpLabels } from '../../types/chamadaApi';
import type { ChamadaApi, MetodoHttp } from '../../types/chamadaApi';
import ChamadaApiFormModal from '../../components/modals/ChamadaApiFormModal';

const metodoVariantMap: Record<number, string> = {
  0: 'success',
  1: 'default',
  2: 'warning',
  3: 'secondary',
  4: 'destructive',
};

export default function ChamadasApi() {
  const { t } = useI18n();
  const [chamadas, setChamadas] = useState<ChamadaApi[]>([]);
  const [selectedChamadas, setSelectedChamadas] = useState<ChamadaApi[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingChamada, setEditingChamada] = useState<ChamadaApi | null>(null);

  const { execute: fetchChamadas, loading } = useApi<PaginatedResult<ChamadaApi>>({
    showErrorMessage: true,
  });

  const { execute: deleteChamadas } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('apiCall.list.removed'), variant: 'success' });
    },
  });

  const loadChamadas = async () => {
    const result = await fetchChamadas(() => chamadaApiService.getAll());
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
      try {
        const resp = await chamadaApiService.getById(selectedChamadas[0].id);
        setEditingChamada(resp.data ?? selectedChamadas[0]);
      } catch {
        setEditingChamada(selectedChamadas[0]);
      }
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const chamada of selectedChamadas) {
      await deleteChamadas(() => chamadaApiService.delete(chamada.id));
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

  const columns: DataTableColumn<ChamadaApi>[] = [
    { key: 'nome', title: t('common.column.name'), dataIndex: 'nome' },
    {
      key: 'metodo',
      title: t('common.column.method'),
      dataIndex: 'metodo',
      render: (value: MetodoHttp) => (
        <Badge variant={(metodoVariantMap[value] || 'outline') as 'success' | 'default' | 'warning' | 'secondary' | 'destructive'}>
          {MetodoHttpLabels[value] || '-'}
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
