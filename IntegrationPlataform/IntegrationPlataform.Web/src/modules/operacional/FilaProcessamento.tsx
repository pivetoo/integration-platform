import { useState, useEffect } from 'react';
import { ListOrdered } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, toast } from 'archon-ui';
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
      toast({ title: 'Removido', description: 'Item da fila removido com sucesso', variant: 'success' });
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
      key: 'prioridade',
      title: 'Prioridade',
      dataIndex: 'prioridade',
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: StatusProcessamento) => (
        <Badge variant={(statusVariantMap[value] || 'outline') as 'warning' | 'info' | 'success' | 'destructive' | 'secondary'}>
          {StatusProcessamentoLabels[value] || '-'}
        </Badge>
      ),
    },
    {
      key: 'agendamento',
      title: 'Agendamento',
      dataIndex: 'agendamento',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'criadoEm',
      title: 'Criado em',
      dataIndex: 'criadoEm',
      render: (value: string) => formatDateTime(value),
    },
  ];

  return (
    <PageLayout
      title="Fila de Processamento"
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
        emptyText="Nenhum item na fila"
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Excluir da Fila"
        description={`Tem certeza que deseja excluir ${selectedItens.length} item(ns) da fila?`}
        confirmText="Excluir"
        cancelText="Cancelar"
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
