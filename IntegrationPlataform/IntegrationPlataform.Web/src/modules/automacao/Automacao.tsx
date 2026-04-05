import { useEffect, useState } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { rotinaPipelineService } from '../../services/rotinaPipelineService';
import type { RotinaPipeline } from '../../types/rotinaPipeline';
import type { Conector } from '../../types/conector';
import type { Pipeline } from '../../types/pipeline';
import RotinaPipelineFormModal from '../../components/modals/RotinaPipelineFormModal';

function formatDateTime(dateStr?: string): string {
  if (!dateStr) {
    return '-';
  }

  return new Date(dateStr).toLocaleString('pt-BR');
}

export default function Automacao() {
  const { t } = useI18n();
  const [rotinas, setRotinas] = useState<RotinaPipeline[]>([]);
  const [selectedRotinas, setSelectedRotinas] = useState<RotinaPipeline[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingRotina, setEditingRotina] = useState<RotinaPipeline | null>(null);

  const { execute: fetchRotinas, loading } = useApi<PaginatedResult<RotinaPipeline>>({
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
    const result = await fetchRotinas(() => rotinaPipelineService.getAll());
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
      await deleteRotinas(() => rotinaPipelineService.delete(rotina.id));
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

  const columns: DataTableColumn<RotinaPipeline>[] = [
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
      key: 'intervaloMinutos',
      title: t('automation.list.intervalMinutes'),
      dataIndex: 'intervaloMinutos',
    },
    {
      key: 'ativo',
      title: t('common.column.active'),
      dataIndex: 'ativo',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.boolean.yes') : t('common.boolean.no')}
        </Badge>
      ),
    },
    {
      key: 'proximaExecucao',
      title: t('automation.list.nextExecution'),
      dataIndex: 'proximaExecucao',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'ultimaExecucao',
      title: t('automation.list.lastExecution'),
      dataIndex: 'ultimaExecucao',
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

      <RotinaPipelineFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        rotina={editingRotina}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
