import { useEffect, useMemo, useState } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { pipelineRoutineService } from '../../../services/pipelineRoutineService';
import type { PipelineRoutine } from '../../../types/pipelineRoutine';
import type { Conector } from '../../../types/connector';
import type { Pipeline } from '../../../types/pipeline';
import PipelineRoutineFormModal from '../../../components/modals/PipelineRoutineFormModal';
import { formatDateTime } from '../../../utils/formatters';

export default function PipelineRoutines() {
  const { t } = useI18n();
  const [rotinas, setRotinas] = useState<PipelineRoutine[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedRotinas, setSelectedRotinas] = useState<PipelineRoutine[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingRotina, setEditingRotina] = useState<PipelineRoutine | null>(null);

  const { execute: fetchRotinas, loading } = useApi<PaginatedResult<PipelineRoutine>>({
    showErrorMessage: true,
  });

  const { execute: deleteRotinas } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('automation.list.removed'), variant: 'success' });
    },
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
      await deleteRotinas(() => pipelineRoutineService.delete(rotina.id));
    }

    setIsConfirmOpen(false);
    setSelectedRotinas([]);
    void loadRotinas();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingRotina(null);
    setSelectedRotinas([]);
    void loadRotinas();
  };

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
        <Badge variant={value ? 'success' : 'destructive'}>
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
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadRotinas()}
      selectedRowsCount={selectedRotinas.length}
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
        selectable
        selectedRows={selectedRotinas}
        onSelectionChange={setSelectedRotinas}
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
        description={t('automation.list.deleteDescription').replace('{0}', String(selectedRotinas.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
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
