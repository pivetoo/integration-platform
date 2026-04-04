import { useEffect, useState } from 'react';
import { Clock3 } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, toast } from 'd-rts';
import type { DataTableColumn, PaginatedResult } from 'd-rts';
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
      toast({ title: 'Removido', description: 'Rotina removida com sucesso', variant: 'success' });
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
      title: 'Conector',
      dataIndex: 'conector',
      render: (value: Conector) => value?.nome || '-',
    },
    {
      key: 'pipeline',
      title: 'Pipeline',
      dataIndex: 'pipeline',
      render: (value: Pipeline) => value?.nome || '-',
    },
    {
      key: 'intervaloMinutos',
      title: 'Intervalo (min)',
      dataIndex: 'intervaloMinutos',
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
      key: 'proximaExecucao',
      title: 'Próxima execução',
      dataIndex: 'proximaExecucao',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'ultimaExecucao',
      title: 'Última execução',
      dataIndex: 'ultimaExecucao',
      render: (value: string) => formatDateTime(value),
    },
  ];

  return (
    <PageLayout
      title="Automação"
      icon={<Clock3 size={24} />}
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
        emptyText="Nenhuma rotina encontrada"
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Excluir Rotina"
        description={`Tem certeza que deseja excluir ${selectedRotinas.length} rotina(s)?`}
        confirmText="Excluir"
        cancelText="Cancelar"
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
