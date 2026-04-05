import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { execucaoService } from '../../services/execucaoService';
import { ExecutionTypeLabels, ExecutionStatusLabels } from '../../types/execucao';
import type { Execution, ExecutionType, ExecutionStatus } from '../../types/execucao';
import type { Conector } from '../../types/conector';
import type { Pipeline } from '../../types/pipeline';
import ExecucaoDetalheModal from '../../components/modals/ExecucaoDetalheModal';

const statusVariantMap: Record<number, string> = {
  1: 'warning',
  2: 'success',
  3: 'destructive',
  4: 'secondary',
};

function formatDuracao(ms?: number): string {
  if (ms == null) {
    return '-';
  }
  if (ms < 1000) {
    return `${ms}ms`;
  }
  const seconds = Math.floor(ms / 1000);
  if (seconds < 60) {
    return `${seconds}s`;
  }
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}m ${remainingSeconds}s`;
}

function formatDateTime(dateStr?: string): string {
  if (!dateStr) {
    return '-';
  }
  return new Date(dateStr).toLocaleString('pt-BR');
}

export default function Execucoes() {
  const { t } = useI18n();
  const [executions, setExecutions] = useState<Execution[]>([]);
  const [selectedExecution, setSelectedExecution] = useState<Execution | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  const { execute: fetchExecutions, loading } = useApi<PaginatedResult<Execution>>({
    showErrorMessage: true,
  });

  const loadExecutions = async () => {
    const result = await fetchExecutions(() => execucaoService.getAll());
    if (result) {
      setExecutions(result.data);
    }
  };

  useEffect(() => {
    loadExecutions();
  }, []);

  const columns: DataTableColumn<Execution>[] = [
    {
      key: 'type',
      title: t('common.column.type'),
      dataIndex: 'type',
      render: (value: ExecutionType) => ExecutionTypeLabels[value] || '-',
    },
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
      key: 'status',
      title: t('common.column.status'),
      dataIndex: 'status',
      render: (value: ExecutionStatus) => (
        <Badge variant={(statusVariantMap[value] || 'outline') as 'warning' | 'success' | 'destructive' | 'secondary'}>
          {ExecutionStatusLabels[value] || '-'}
        </Badge>
      ),
    },
    {
      key: 'startedAt',
      title: t('common.column.startedAt'),
      dataIndex: 'startedAt',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'duration',
      title: t('common.column.duration'),
      dataIndex: 'duration',
      render: (value: number) => formatDuracao(value),
    },
  ];

  const handleRowClick = (execution: Execution) => {
    setSelectedExecution(execution);
    setModalOpen(true);
  };

  const handleModalOpenChange = (open: boolean) => {
    setModalOpen(open);
    if (!open) {
      setSelectedExecution(null);
    }
  };

  return (
    <PageLayout
      title={t('execution.list.title')}
      onRefresh={loadExecutions}
    >
      <DataTable
        columns={columns}
        data={executions}
        rowKey="id"
        emptyText={t('execution.list.empty')}
        loading={loading}
        onRowClick={handleRowClick}
      />

      <ExecucaoDetalheModal
        open={modalOpen}
        onOpenChange={handleModalOpenChange}
        execution={selectedExecution}
      />
    </PageLayout>
  );
}
