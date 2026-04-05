import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { execucaoService } from '../../services/execucaoService';
import { TipoExecucaoLabels, StatusExecucaoLabels } from '../../types/execucao';
import type { Execucao, TipoExecucao, StatusExecucao as StatusExecucaoType } from '../../types/execucao';
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
  const [execucoes, setExecucoes] = useState<Execucao[]>([]);
  const [selectedExecucao, setSelectedExecucao] = useState<Execucao | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  const { execute: fetchExecucoes, loading } = useApi<PaginatedResult<Execucao>>({
    showErrorMessage: true,
  });

  const loadExecucoes = async () => {
    const result = await fetchExecucoes(() => execucaoService.getAll());
    if (result) {
      setExecucoes(result.data);
    }
  };

  useEffect(() => {
    loadExecucoes();
  }, []);

  const columns: DataTableColumn<Execucao>[] = [
    {
      key: 'tipo',
      title: t('common.column.type'),
      dataIndex: 'tipo',
      render: (value: TipoExecucao) => TipoExecucaoLabels[value] || '-',
    },
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
      key: 'status',
      title: t('common.column.status'),
      dataIndex: 'status',
      render: (value: StatusExecucaoType) => (
        <Badge variant={(statusVariantMap[value] || 'outline') as 'warning' | 'success' | 'destructive' | 'secondary'}>
          {StatusExecucaoLabels[value] || '-'}
        </Badge>
      ),
    },
    {
      key: 'iniciadoEm',
      title: t('common.column.startedAt'),
      dataIndex: 'iniciadoEm',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'duracao',
      title: t('common.column.duration'),
      dataIndex: 'duracao',
      render: (value: number) => formatDuracao(value),
    },
  ];

  const handleRowClick = (execucao: Execucao) => {
    setSelectedExecucao(execucao);
    setModalOpen(true);
  };

  const handleModalOpenChange = (open: boolean) => {
    setModalOpen(open);
    if (!open) {
      setSelectedExecucao(null);
    }
  };

  return (
    <PageLayout
      title={t('execution.list.title')}
      onRefresh={loadExecucoes}
    >
      <DataTable
        columns={columns}
        data={execucoes}
        rowKey="id"
        emptyText={t('execution.list.empty')}
        loading={loading}
        onRowClick={handleRowClick}
      />

      <ExecucaoDetalheModal
        open={modalOpen}
        onOpenChange={handleModalOpenChange}
        execucao={selectedExecucao}
      />
    </PageLayout>
  );
}
