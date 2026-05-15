import { useEffect, useMemo, useState } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { processingQueueService } from '../../../services/processingQueueService';
import { ProcessingStatusLabels } from '../../../types/processingQueue';
import type { ProcessingQueueItem, ProcessingStatus } from '../../../types/processingQueue';
import type { Conector } from '../../../types/connector';
import type { Pipeline } from '../../../types/pipeline';
import ProcessingQueueFormModal from '../../../components/modals/ProcessingQueueFormModal';
import { formatDateTime } from '../../../utils/formatters';

const statusVariantMap: Record<number, string> = {
  0: 'warning',
  1: 'info',
  2: 'success',
  3: 'destructive',
  4: 'secondary',
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
  const [selectedItens, setSelectedItens] = useState<ProcessingQueueItem[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);

  const { execute: fetchItens, loading } = useApi<PaginatedResult<ProcessingQueueItem>>({
    showErrorMessage: true,
  });

  const { execute: deleteItens } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('processingQueue.list.removed'), variant: 'success' });
    },
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

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const item of selectedItens) {
      await deleteItens(() => processingQueueService.delete(item.id));
    }
    setIsConfirmOpen(false);
    setSelectedItens([]);
    void loadItens();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setSelectedItens([]);
    void loadItens();
  };

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
        <Badge variant={(statusVariantMap[value] || 'outline') as 'warning' | 'info' | 'success' | 'destructive' | 'secondary'}>
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
      onDelete={handleDelete}
      onRefresh={() => void loadItens()}
      selectedRowsCount={selectedItens.length}
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
        selectable
        selectedRows={selectedItens}
        onSelectionChange={setSelectedItens}
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
        description={t('processingQueue.list.deleteDescription').replace('{0}', String(selectedItens.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <ProcessingQueueFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
