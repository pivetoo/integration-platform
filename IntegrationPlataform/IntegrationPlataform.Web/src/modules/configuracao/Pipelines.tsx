import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ExternalLink } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { pipelineService } from '../../services/pipelineService';
import type { Pipeline } from '../../types/pipeline';
import type { Integracao } from '../../types/integracao';
import PipelineFormModal from '../../components/modals/PipelineFormModal';

export default function Pipelines() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [pipelines, setPipelines] = useState<Pipeline[]>([]);
  const [selectedPipelines, setSelectedPipelines] = useState<Pipeline[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingPipeline, setEditingPipeline] = useState<Pipeline | null>(null);

  const { execute: fetchPipelines, loading } = useApi<PaginatedResult<Pipeline>>({
    showErrorMessage: true,
  });

  const { execute: deletePipelines } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('pipeline.list.removed'), variant: 'success' });
    },
  });

  const loadPipelines = async () => {
    const result = await fetchPipelines(() => pipelineService.getAll());
    if (result) {
      setPipelines(result.data);
    }
  };

  useEffect(() => {
    loadPipelines();
  }, []);

  const handleAdd = () => {
    setEditingPipeline(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selectedPipelines.length === 1) {
      setEditingPipeline(selectedPipelines[0]);
      setIsFormOpen(true);
    }
  };

  const handleRowDoubleClick = (pipeline: Pipeline) => {
    navigate(`/pipelines/${pipeline.id}`);
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const pipeline of selectedPipelines) {
      await deletePipelines(() => pipelineService.delete(pipeline.id));
    }
    setIsConfirmOpen(false);
    setSelectedPipelines([]);
    loadPipelines();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingPipeline(null);
    setSelectedPipelines([]);
    loadPipelines();
  };

  const columns: DataTableColumn<Pipeline>[] = [
    { key: 'identificador', title: t('common.column.identifier'), dataIndex: 'identificador' },
    { key: 'nome', title: t('common.column.name'), dataIndex: 'nome' },
    {
      key: 'integracao',
      title: t('common.column.integration'),
      dataIndex: 'integracao',
      render: (value: Integracao) => value?.nome || '-',
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
      key: 'acoes',
      title: t('common.column.actions'),
      width: 130,
      render: (_value, pipeline: Pipeline) => (
        <button
          type="button"
          onClick={() => navigate(`/pipelines/${pipeline.id}`)}
          className="inline-flex items-center gap-1.5 rounded-md border border-border px-2.5 py-1.5 text-xs font-medium text-foreground transition-colors hover:bg-accent"
        >
          <ExternalLink size={14} />
          {t('common.action.open')}
        </button>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('pipeline.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadPipelines}
      selectedRowsCount={selectedPipelines.length}
    >
      <DataTable
        columns={columns}
        data={pipelines}
        rowKey="id"
        selectedRows={selectedPipelines}
        onSelectionChange={setSelectedPipelines}
        onRowDoubleClick={handleRowDoubleClick}
        emptyText={t('pipeline.list.empty')}
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('pipeline.list.deleteTitle')}
        description={t('pipeline.list.deleteDescription').replace('{0}', String(selectedPipelines.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <PipelineFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        pipeline={editingPipeline}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
