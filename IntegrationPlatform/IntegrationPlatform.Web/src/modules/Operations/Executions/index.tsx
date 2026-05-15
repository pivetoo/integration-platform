import { useEffect, useMemo, useState } from 'react';
import { PageLayout, DataTable, Badge, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { executionService } from '../../../services/executionService';
import { ExecutionTypeLabels, ExecutionStatusLabels } from '../../../types/execution';
import type { Execution, ExecutionType, ExecutionStatus } from '../../../types/execution';
import type { Conector } from '../../../types/connector';
import type { Pipeline } from '../../../types/pipeline';
import ExecutionDetailModal from '../../../components/modals/ExecutionDetailModal';
import { formatDateTime, formatDuration } from '../../../utils/formatters';

const statusVariantMap: Record<number, string> = {
  1: 'warning',
  2: 'success',
  3: 'destructive',
  4: 'secondary',
};

export default function Executions() {
  const { t } = useI18n();
  const [executions, setExecutions] = useState<Execution[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedExecution, setSelectedExecution] = useState<Execution | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  const { execute: fetchExecutions, loading } = useApi<PaginatedResult<Execution>>({
    showErrorMessage: true,
  });

  const loadExecutions = async () => {
    const result = await fetchExecutions(() =>
      executionService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      const filtered = result.data.filter((e: Execution) => {
        if (statusFilter === '') return true;
        return String(e.status) === statusFilter;
      });
      setExecutions(filtered);
      setTotalCount(result.total ?? 0);
    }
  };

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timeout);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter]);

  useEffect(() => {
    void loadExecutions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, statusFilter]);

  const filterSections: FilterSection[] = useMemo(
    () => [
      {
        key: 'status',
        label: t('common.column.status'),
        value: statusFilter,
        onChange: setStatusFilter,
        options: Object.entries(ExecutionStatusLabels).map(([value, label]) => ({
          value: String(value),
          label: String(label),
        })),
        allLabel: t('common.filter.all'),
      },
    ],
    [statusFilter, t],
  );

  const clearFilters = () => setStatusFilter('');

  const columns: DataTableColumn<Execution>[] = [
    {
      key: 'type',
      title: t('common.column.type'),
      dataIndex: 'type',
      width: 130,
      render: (value: ExecutionType) => ExecutionTypeLabels[value] || '-',
    },
    {
      key: 'connector',
      title: t('common.column.connector'),
      dataIndex: 'connector',
      hiddenBelow: 'sm',
      render: (value: Conector) => value?.name || '-',
    },
    {
      key: 'pipeline',
      title: t('common.column.pipeline'),
      dataIndex: 'pipeline',
      hiddenBelow: 'md',
      render: (value: Pipeline) => value?.name || '-',
    },
    {
      key: 'status',
      title: t('common.column.status'),
      dataIndex: 'status',
      width: 120,
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
      hiddenBelow: 'lg',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'duration',
      title: t('common.column.duration'),
      dataIndex: 'duration',
      hiddenBelow: 'lg',
      render: (value: number) => formatDuration(value),
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
      onRefresh={() => void loadExecutions()}
    >
      <TableToolbar
        searchValue={search}
        onSearchChange={setSearch}
        searchPlaceholder={t('common.action.search')}
        rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
        className="mb-3"
      />

      <DataTable
        columns={columns}
        data={executions}
        rowKey="id"
        loading={loading}
        emptyText={t('common.state.empty')}
        onRowClick={handleRowClick}
        pageSize={pageSize}
        pageSizeOptions={[10, 20, 50]}
        totalCount={totalCount}
        page={page}
        onPageChange={setPage}
        onPageSizeChange={(s) => {
          setPageSize(s);
          setPage(1);
        }}
      />

      <ExecutionDetailModal
        open={modalOpen}
        onOpenChange={handleModalOpenChange}
        execution={selectedExecution}
      />
    </PageLayout>
  );
}
