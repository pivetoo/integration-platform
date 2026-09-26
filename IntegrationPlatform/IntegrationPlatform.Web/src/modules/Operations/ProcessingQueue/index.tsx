import { useEffect, useMemo, useState } from 'react';
import { Trash2 } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { processingQueueService } from '../../../services/processingQueueService';
import { ProcessingStatusLabels, ProcessingStatus } from '../../../types/processingQueue';
import type { ProcessingQueueItem } from '../../../types/processingQueue';
import type { Conector } from '../../../types/connector';
import type { Pipeline } from '../../../types/pipeline';
import ProcessingQueueFormModal from '../../../components/modals/ProcessingQueueFormModal';
import { formatDateTime } from '../../../utils/formatters';
import { useBulkRun } from '../../../lib/useBulkRun';

const statusVariantMap: Record<number, 'soft-warning' | 'soft-info' | 'soft-success' | 'soft-destructive' | 'soft-neutral'> = {
  [ProcessingStatus.Pending]: 'soft-warning',
  [ProcessingStatus.Processing]: 'soft-info',
  [ProcessingStatus.Completed]: 'soft-success',
  [ProcessingStatus.Error]: 'soft-destructive',
  [ProcessingStatus.Cancelled]: 'soft-neutral',
};

export default function ProcessingQueue() {
  const { t } = useI18n();
  const [itens, setItens] = useState<ProcessingQueueItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedRows, setSelectedRows] = useState<ProcessingQueueItem[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<ProcessingQueueItem[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchItens, loading } = useApi<PaginatedResult<ProcessingQueueItem>>({
    showErrorMessage: true,
  });

  const loadItens = async () => {
    const result = await fetchItens(() =>
      processingQueueService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      const filtered = result.data.filter((i: ProcessingQueueItem) => {
        if (statusFilter === '') return true;
        return String(i.status) === statusFilter;
      });
      setItens(filtered);
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
    void loadItens();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, statusFilter]);

  const filterSections: FilterSection[] = useMemo(
    () => [
      {
        key: 'status',
        label: t('common.column.status'),
        value: statusFilter,
        onChange: setStatusFilter,
        options: Object.entries(ProcessingStatusLabels).map(([value, label]) => ({
          value: String(value),
          label: String(label),
        })),
        allLabel: t('common.filter.all'),
      },
    ],
    [statusFilter, t],
  );

  const clearFilters = () => setStatusFilter('');

  const handleAdd = () => {
    setIsFormOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (item) => processingQueueService.delete(item.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadItens();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setSelectedRows([]);
    void loadItens();
  };

  const rowActions: DataTableRowAction<ProcessingQueueItem>[] = [
    {
      key: 'delete',
      label: t('common.action.delete'),
      icon: <Trash2 className="h-4 w-4" />,
      variant: 'danger',
      onClick: (row) => {
        setItemsToDelete([row]);
        setIsConfirmOpen(true);
      },
    },
  ];

  const bulkActions: DataTableBulkAction<ProcessingQueueItem>[] = [
    {
      key: 'delete',
      label: t('common.action.delete'),
      icon: <Trash2 className="h-4 w-4" />,
      variant: 'danger',
      disabled: bulkRunning,
      onClick: (rows) => {
        setItemsToDelete(rows);
        setIsConfirmOpen(true);
      },
    },
  ];

  const columns: DataTableColumn<ProcessingQueueItem>[] = [
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
      hiddenBelow: 'sm',
      render: (value: Pipeline) => value?.name || '-',
    },
    {
      key: 'priority',
      title: t('common.column.priority'),
      dataIndex: 'priority',
      width: 100,
      hiddenBelow: 'md',
    },
    {
      key: 'status',
      title: t('common.column.status'),
      dataIndex: 'status',
      width: 130,
      render: (value: ProcessingStatus) => (
        <Badge dot variant={statusVariantMap[value] || 'soft-neutral'}>
          {ProcessingStatusLabels[value] || '-'}
        </Badge>
      ),
    },
    {
      key: 'scheduledAt',
      title: t('common.column.scheduledAt'),
      dataIndex: 'scheduledAt',
      hiddenBelow: 'lg',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'createdAt',
      title: t('common.column.createdAt'),
      dataIndex: 'createdAt',
      hiddenBelow: 'lg',
      render: (value: string) => formatDateTime(value),
    },
  ];

  return (
    <PageLayout
      title={t('processingQueue.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadItens()}
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
        data={itens}
        rowKey="id"
        loading={loading}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        rowActions={rowActions}
        bulkActions={bulkActions}
        emptyText={t('common.state.empty')}
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

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('processingQueue.list.deleteTitle')}
        description={t('processingQueue.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
      />

      <ProcessingQueueFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
