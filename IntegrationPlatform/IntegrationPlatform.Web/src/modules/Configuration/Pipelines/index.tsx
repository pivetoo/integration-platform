import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Eye, Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { pipelineService } from '../../../services/pipelineService';
import type { Pipeline } from '../../../types/pipeline';
import type { Integration } from '../../../types/integration';
import PipelineFormModal from '../../../components/modals/PipelineFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';

export default function Pipelines() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [pipelines, setPipelines] = useState<Pipeline[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedRows, setSelectedRows] = useState<Pipeline[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<Pipeline[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingPipeline, setEditingPipeline] = useState<Pipeline | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchPipelines, loading } = useApi<PaginatedResult<Pipeline>>({
    showErrorMessage: true,
  });

  const loadPipelines = async () => {
    const result = await fetchPipelines(() =>
      pipelineService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      const filtered = result.data.filter((p: Pipeline) => {
        if (statusFilter === 'active') return p.isActive;
        if (statusFilter === 'inactive') return !p.isActive;
        return true;
      });
      setPipelines(filtered);
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
    void loadPipelines();
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
    setEditingPipeline(null);
    setIsFormOpen(true);
  };

  const handleRowClick = (pipeline: Pipeline) => {
    navigate(`/pipelines/${pipeline.id}`);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (pipeline) => pipelineService.delete(pipeline.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadPipelines();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingPipeline(null);
    setSelectedRows([]);
    void loadPipelines();
  };

  const rowActions: DataTableRowAction<Pipeline>[] = [
    {
      key: 'details',
      label: t('common.action.details') !== 'common.action.details' ? t('common.action.details') : 'Detalhes',
      icon: <Eye className="h-4 w-4" />,
      onClick: (row) => navigate(`/pipelines/${row.id}`),
    },
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingPipeline(row);
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

  const bulkActions: DataTableBulkAction<Pipeline>[] = [
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

  const columns: DataTableColumn<Pipeline>[] = [
    { key: 'identifier', title: t('common.column.identifier'), dataIndex: 'identifier', hiddenBelow: 'md' },
    { key: 'name', title: t('common.column.name'), dataIndex: 'name', sortable: true },
    {
      key: 'integration',
      title: t('common.column.integration'),
      dataIndex: 'integration',
      hiddenBelow: 'sm',
      render: (value: Integration) => value?.name || '-',
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
  ];

  return (
    <PageLayout
      title={t('pipeline.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadPipelines()}
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
        data={pipelines}
        rowKey="id"
        loading={loading}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        rowActions={rowActions}
        bulkActions={bulkActions}
        onRowClick={handleRowClick}
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
        title={t('pipeline.list.deleteTitle')}
        description={t('pipeline.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
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
