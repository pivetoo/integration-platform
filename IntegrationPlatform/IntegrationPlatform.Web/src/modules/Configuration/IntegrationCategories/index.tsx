import { useEffect, useMemo, useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, ConfirmModal, Badge, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { integrationCategoryService } from '../../../services/integrationCategoryService';
import type { IntegrationCategory } from '../../../types/integrationCategory';
import CategoriaIntegracaoFormModal from '../../../components/modals/IntegrationCategoryFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';

export default function CategoriasIntegracao() {
  const { t } = useI18n();
  const [categorias, setCategorias] = useState<IntegrationCategory[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('active');
  const [selectedRows, setSelectedRows] = useState<IntegrationCategory[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<IntegrationCategory[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingCategoria, setEditingCategoria] = useState<IntegrationCategory | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchCategorias, loading } = useApi<PaginatedResult<IntegrationCategory>>({
    showErrorMessage: true,
  });

  const loadCategorias = async () => {
    const result = await fetchCategorias(() =>
      integrationCategoryService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      const filtered = result.data.filter((c: IntegrationCategory) => {
        if (statusFilter === 'active') return c.isActive;
        if (statusFilter === 'inactive') return !c.isActive;
        return true;
      });
      setCategorias(filtered);
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
    void loadCategorias();
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
    setEditingCategoria(null);
    setIsFormOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (categoria) => integrationCategoryService.delete(categoria.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadCategorias();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingCategoria(null);
    setSelectedRows([]);
    void loadCategorias();
  };

  const rowActions: DataTableRowAction<IntegrationCategory>[] = [
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingCategoria(row);
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

  const bulkActions: DataTableBulkAction<IntegrationCategory>[] = [
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

  const columns: DataTableColumn<IntegrationCategory>[] = [
    {
      key: 'name',
      title: t('common.column.name'),
      dataIndex: 'name',
      sortable: true,
      render: (value: string) => <span className="font-medium">{value}</span>,
    },
    {
      key: 'identifier',
      title: t('integration.category.column.identifier'),
      dataIndex: 'identifier',
      width: 200,
      render: (value: string) => (
        <code className="rounded bg-muted px-1.5 py-0.5 text-xs">{value}</code>
      ),
    },
    { key: 'description', title: t('common.column.description'), dataIndex: 'description', hiddenBelow: 'md' },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      width: 120,
      render: (value: boolean) => (
        <Badge dot variant={value ? 'soft-success' : 'soft-neutral'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('integration.category.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadCategorias()}
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
        data={categorias}
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
        title={t('integration.category.list.deleteTitle')}
        description={t('integration.category.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
      />

      <CategoriaIntegracaoFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        categoria={editingCategoria}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
