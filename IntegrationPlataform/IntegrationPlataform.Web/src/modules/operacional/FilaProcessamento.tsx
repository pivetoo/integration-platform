import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { filaProcessamentoService } from '../../services/filaProcessamentoService';
import { StatusProcessamentoLabels } from '../../types/filaProcessamento';
import type { FilaProcessamento as FilaProcessamentoType, StatusProcessamento } from '../../types/filaProcessamento';
import type { Conector } from '../../types/conector';
import type { Pipeline } from '../../types/pipeline';
import FilaFormModal from '../../components/modals/FilaFormModal';

const statusVariantMap: Record<number, string> = {
  0: 'warning',
  1: 'info',
  2: 'success',
  3: 'destructive',
  4: 'secondary',
};

function formatDateTime(dateStr?: string): string {
  if (!dateStr) {
    return '-';
  }
  return new Date(dateStr).toLocaleString('pt-BR');
}

export default function FilaProcessamento() {
  const { t } = useI18n();
  const [itens, setItens] = useState<FilaProcessamentoType[]>([]);
  const [selectedItens, setSelectedItens] = useState<FilaProcessamentoType[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);

  const { execute: fetchItens, loading } = useApi<PaginatedResult<FilaProcessamentoType>>({
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
    const result = await fetchItens(() => filaProcessamentoService.getAll());
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
      await deleteItens(() => filaProcessamentoService.delete(item.id));
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

  const columns: DataTableColumn<FilaProcessamentoType>[] = [
    {
      key: 'conector',
      title: t('common.column.connector'),
      dataIndex: 'conector',
      render: (value: Conector) => value?.nome || '-',
    },
    {
      key: 'pipeline',
      title: t('common.column.pipeline'),
      dataIndex: 'pipeline',
      render: (value: Pipeline) => value?.nome || '-',
    },
    {
      key: 'prioridade',
      title: t('common.column.priority'),
      dataIndex: 'prioridade',
    },
    {
      key: 'status',
      title: t('common.column.status'),
      dataIndex: 'status',
      render: (value: StatusProcessamento) => (
        <Badge variant={(statusVariantMap[value] || 'outline') as 'warning' | 'info' | 'success' | 'destructive' | 'secondary'}>
          {StatusProcessamentoLabels[value] || '-'}
        </Badge>
      ),
    },
    {
      key: 'agendamento',
      title: t('common.column.scheduledAt'),
      dataIndex: 'agendamento',
      render: (value: string) => formatDateTime(value),
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

      <FilaFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
