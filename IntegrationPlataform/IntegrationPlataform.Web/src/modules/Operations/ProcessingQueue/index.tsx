import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { processingQueueService } from '../../../services/processingQueueService';
import { ProcessingStatusLabels } from '../../../types/processingQueue';
import type { ProcessingQueueItem, ProcessingStatus } from '../../../types/processingQueue';
import type { Conector } from '../../../types/connector';
import type { Pipeline } from '../../../types/pipeline';
import ProcessingQueueFormModal from '../../../components/modals/ProcessingQueueFormModal';
import { formatDateTime } from '../../../utils/formatters';

const statusVariantMap: Record<number, string> = {
  0: 'warning',
  1: 'info',
  2: 'success',
  3: 'destructive',
  4: 'secondary',
};

export default function ProcessingQueue() {
  const { t } = useI18n();
  const [itens, setItens] = useState<ProcessingQueueItem[]>([]);
  const [selectedItens, setSelectedItens] = useState<ProcessingQueueItem[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);

  const { execute: fetchItens, loading } = useApi<PaginatedResult<ProcessingQueueItem>>({
    showErrorMessage: true,
  });

  const { execute: deleteItens } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('processingQueue.list.removed'), variant: 'success' });
    },
  });

  const loadItens = async () => {
    const result = await fetchItens(() => processingQueueService.getAll());
    if (result) {
      setItens(result.data);
    }
  };

  useEffect(() => {
    loadItens();
  }, []);

  const handleAdd = () => {
    setIsFormOpen(true);
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const item of selectedItens) {
      await deleteItens(() => processingQueueService.delete(item.id));
    }
    setIsConfirmOpen(false);
    setSelectedItens([]);
    loadItens();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setSelectedItens([]);
    loadItens();
  };

  const columns: DataTableColumn<ProcessingQueueItem>[] = [
    {
      key: 'connector',
      title: t('common.column.connector'),
      dataIndex: 'connector',
      render: (value: Conector) => value?.name || '-',
    },
    {
      key: 'pipeline',
      title: t('common.column.pipeline'),
      dataIndex: 'pipeline',
      render: (value: Pipeline) => value?.name || '-',
    },
    {
      key: 'priority',
      title: t('common.column.priority'),
      dataIndex: 'priority',
    },
    {
      key: 'status',
      title: t('common.column.status'),
      dataIndex: 'status',
      render: (value: ProcessingStatus) => (
        <Badge variant={(statusVariantMap[value] || 'outline') as 'warning' | 'info' | 'success' | 'destructive' | 'secondary'}>
          {ProcessingStatusLabels[value] || '-'}
        </Badge>
      ),
    },
    {
      key: 'scheduledAt',
      title: t('common.column.scheduledAt'),
      dataIndex: 'scheduledAt',
      render: (value: string) => formatDateTime(value),
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
      title={t('processingQueue.list.title')}
      onAdd={handleAdd}
      onDelete={handleDelete}
      onRefresh={loadItens}
      selectedRowsCount={selectedItens.length}
    >
      <DataTable
        columns={columns}
        data={itens}
        rowKey="id"
        selectedRows={selectedItens}
        onSelectionChange={setSelectedItens}
        emptyText={t('processingQueue.list.empty')}
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('processingQueue.list.deleteTitle')}
        description={t('processingQueue.list.deleteDescription').replace('{0}', String(selectedItens.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <ProcessingQueueFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
