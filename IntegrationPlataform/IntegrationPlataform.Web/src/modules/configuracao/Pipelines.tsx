import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ExternalLink, Workflow } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { pipelineService } from '../../services/pipelineService';
import type { Pipeline } from '../../types/pipeline';
import type { Integracao } from '../../types/integracao';
import PipelineFormModal from '../../components/modals/PipelineFormModal';

export default function Pipelines() {
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
      toast({ title: 'Removido', description: 'Pipeline removido com sucesso', variant: 'success' });
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
    { key: 'identificador', title: 'Identificador', dataIndex: 'identificador' },
    { key: 'nome', title: 'Nome', dataIndex: 'nome' },
    {
      key: 'integracao',
      title: 'Integração',
      dataIndex: 'integracao',
      render: (value: Integracao) => value?.nome || '-',
    },
    {
      key: 'ativo',
      title: 'Ativo',
      dataIndex: 'ativo',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Sim' : 'Não'}
        </Badge>
      ),
    },
    {
      key: 'acoes',
      title: 'Ações',
      width: 130,
      render: (_value, pipeline: Pipeline) => (
        <button
          type="button"
          onClick={() => navigate(`/pipelines/${pipeline.id}`)}
          className="inline-flex items-center gap-1.5 rounded-md border border-border px-2.5 py-1.5 text-xs font-medium text-foreground transition-colors hover:bg-accent"
        >
          <ExternalLink size={14} />
          Abrir
        </button>
      ),
    },
  ];

  return (
    <PageLayout
      title="Pipelines"
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
        emptyText="Nenhum pipeline encontrado"
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Excluir Pipeline"
        description={`Tem certeza que deseja excluir ${selectedPipelines.length} pipeline(s)?`}
        confirmText="Excluir"
        cancelText="Cancelar"
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
