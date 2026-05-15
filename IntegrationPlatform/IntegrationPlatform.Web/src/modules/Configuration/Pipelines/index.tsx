import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ExternalLink } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { pipelineService } from '../../../services/pipelineService';
import type { Pipeline } from '../../../types/pipeline';
import type { Integration } from '../../../types/integration';
import PipelineFormModal from '../../../components/modals/PipelineFormModal';

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
  const [selectedPipelines, setSelectedPipelines] = useState<Pipeline[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingPipeline, setEditingPipeline] = useState<Pipeline | null>(null);

  const { execute: fetchPipelines, loading } = useApi<PaginatedResult<Pipeline>>({
    showErrorMessage: true,
  });

  const { execute: deletePipelines } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('pipeline.list.removed'), variant: 'success' });
    },
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

  const handleEdit = () => {
    if (selectedPipelines.length === 1) {
      setEditingPipeline(selectedPipelines[0]);
      setIsFormOpen(true);
    }
  };

  const handleRowDoubleClick = (pipeline: Pipeline) => {
    navigate(`/pipelines/${pipeline.id}`);
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const pipeline of selectedPipelines) {
      await deletePipelines(() => pipelineService.delete(pipeline.id));
    }
    setIsConfirmOpen(false);
    setSelectedPipelines([]);
    void loadPipelines();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingPipeline(null);
    setSelectedPipelines([]);
    void loadPipelines();
  };

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
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
    {
      key: 'acoes',
      title: t('common.column.actions'),
      width: 130,
      render: (_value, pipeline: Pipeline) => (
        <button
          type="button"
          onClick={() => navigate(`/pipelines/${pipeline.id}`)}
          className="inline-flex items-center gap-1.5 rounded-md border border-border px-2.5 py-1.5 text-xs font-medium text-foreground transition-colors hover:bg-accent"
        >
          <ExternalLink size={14} />
          {t('common.action.open')}
        </button>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('pipeline.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadPipelines()}
      selectedRowsCount={selectedPipelines.length}
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
        selectable
        selectedRows={selectedPipelines}
        onSelectionChange={setSelectedPipelines}
        onRowDoubleClick={handleRowDoubleClick}
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
        description={t('pipeline.list.deleteDescription').replace('{0}', String(selectedPipelines.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
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
