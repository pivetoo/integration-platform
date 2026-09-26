import { useEffect, useMemo, useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { pipelineRoutineService } from '../../../services/pipelineRoutineService';
import type { PipelineRoutine } from '../../../types/pipelineRoutine';
import type { Conector } from '../../../types/connector';
import type { Pipeline } from '../../../types/pipeline';
import PipelineRoutineFormModal from '../../../components/modals/PipelineRoutineFormModal';
import { formatDateTime } from '../../../utils/formatters';
import { useBulkRun } from '../../../lib/useBulkRun';

export default function PipelineRoutines() {
  const { t } = useI18n();
  const [rotinas, setRotinas] = useState<PipelineRoutine[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('active');
  const [selectedRows, setSelectedRows] = useState<PipelineRoutine[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<PipelineRoutine[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingRotina, setEditingRotina] = useState<PipelineRoutine | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchRotinas, loading } = useApi<PaginatedResult<PipelineRoutine>>({
    showErrorMessage: true,
  });

  const loadRotinas = async () => {
    const result = await fetchRotinas(() =>
      pipelineRoutineService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      const filtered = result.data.filter((r: PipelineRoutine) => {
        if (statusFilter === 'active') return r.isActive;
        if (statusFilter === 'inactive') return !r.isActive;
        return true;
      });
      setRotinas(filtered);
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
    void loadRotinas();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, statusFilter]);

  const filterSections: FilterSection[] = useMemo(
    () => [
      {
        key: 'status',
        label: t('common.column.status'),
        value: statusFilter,
        onChange: setStatusFilter,
        options: [
          { value: 'active', label: t('common.filter.activeOnly') },
          { value: 'inactive', label: t('common.filter.inactiveOnly') },
        ],
        allLabel: t('common.filter.all'),
      },
    ],
    [statusFilter, t],
  );

  const clearFilters = () => setStatusFilter('');

  const handleAdd = () => {
    setEditingRotina(null);
    setIsFormOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (rotina) => pipelineRoutineService.delete(rotina.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadRotinas();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingRotina(null);
    setSelectedRows([]);
    void loadRotinas();
  };

  const rowActions: DataTableRowAction<PipelineRoutine>[] = [
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingRotina(row);
        setIsFormOpen(true);
      },
    },
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

  const bulkActions: DataTableBulkAction<PipelineRoutine>[] = [
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

  const columns: DataTableColumn<PipelineRoutine>[] = [
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
      key: 'intervalMinutes',
      title: t('automation.list.intervalMinutes'),
      dataIndex: 'intervalMinutes',
      width: 100,
      hiddenBelow: 'md',
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      width: 110,
      render: (value: boolean) => (
        <Badge dot variant={value ? 'soft-success' : 'soft-neutral'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
    {
      key: 'nextExecution',
      title: t('automation.list.nextExecution'),
      dataIndex: 'nextExecution',
      hiddenBelow: 'lg',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'lastExecution',
      title: t('automation.list.lastExecution'),
      dataIndex: 'lastExecution',
      hiddenBelow: 'lg',
      render: (value: string) => formatDateTime(value),
    },
  ];

  return (
    <PageLayout
      title={t('automation.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadRotinas()}
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
        data={rotinas}
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
        title={t('automation.list.deleteTitle')}
        description={t('automation.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
      />

      <PipelineRoutineFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        rotina={editingRotina}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
