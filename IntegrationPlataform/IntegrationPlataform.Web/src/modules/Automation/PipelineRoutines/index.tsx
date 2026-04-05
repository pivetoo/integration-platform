import { useEffect, useState } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { pipelineRoutineService } from '../../../services/pipelineRoutineService';
import type { PipelineRoutine } from '../../../types/pipelineRoutine';
import type { Conector } from '../../../types/connector';
import type { Pipeline } from '../../../types/pipeline';
import PipelineRoutineFormModal from '../../../components/modals/PipelineRoutineFormModal';
import { formatDateTime } from '../../../utils/formatters';

export default function PipelineRoutines() {
  const { t } = useI18n();
  const [rotinas, setRotinas] = useState<PipelineRoutine[]>([]);
  const [selectedRotinas, setSelectedRotinas] = useState<PipelineRoutine[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingRotina, setEditingRotina] = useState<PipelineRoutine | null>(null);

  const { execute: fetchRotinas, loading } = useApi<PaginatedResult<PipelineRoutine>>({
    showErrorMessage: true,
  });

  const { execute: deleteRotinas } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('automation.list.removed'), variant: 'success' });
    },
  });

  const loadRotinas = async () => {
    const result = await fetchRotinas(() => pipelineRoutineService.getAll());
    if (result) {
      setRotinas(result.data);
    }
  };

  useEffect(() => {
    loadRotinas();
  }, []);

  const handleAdd = () => {
    setEditingRotina(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selectedRotinas.length === 1) {
      setEditingRotina(selectedRotinas[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const rotina of selectedRotinas) {
      await deleteRotinas(() => pipelineRoutineService.delete(rotina.id));
    }

    setIsConfirmOpen(false);
    setSelectedRotinas([]);
    loadRotinas();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingRotina(null);
    setSelectedRotinas([]);
    loadRotinas();
  };

  const columns: DataTableColumn<PipelineRoutine>[] = [
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
      key: 'intervalMinutes',
      title: t('automation.list.intervalMinutes'),
      dataIndex: 'intervalMinutes',
    },
    {
      key: 'isActive',
      title: t('common.column.active'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.boolean.yes') : t('common.boolean.no')}
        </Badge>
      ),
    },
    {
      key: 'nextExecution',
      title: t('automation.list.nextExecution'),
      dataIndex: 'nextExecution',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'lastExecution',
      title: t('automation.list.lastExecution'),
      dataIndex: 'lastExecution',
      render: (value: string) => formatDateTime(value),
    },
  ];

  return (
    <PageLayout
      title={t('automation.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadRotinas}
      selectedRowsCount={selectedRotinas.length}
    >
      <DataTable
        columns={columns}
        data={rotinas}
        rowKey="id"
        selectedRows={selectedRotinas}
        onSelectionChange={setSelectedRotinas}
        emptyText={t('automation.list.empty')}
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('automation.list.deleteTitle')}
        description={t('automation.list.deleteDescription').replace('{0}', String(selectedRotinas.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <PipelineRoutineFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        rotina={editingRotina}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
