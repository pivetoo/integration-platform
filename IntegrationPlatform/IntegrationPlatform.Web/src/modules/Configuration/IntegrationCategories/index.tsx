import { useEffect, useMemo, useState } from 'react';
import { PageLayout, DataTable, ConfirmModal, Badge, FilterPanel, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { integrationCategoryService } from '../../../services/integrationCategoryService';
import type { IntegrationCategory } from '../../../types/integrationCategory';
import CategoriaIntegracaoFormModal from '../../../components/modals/IntegrationCategoryFormModal';

export default function CategoriasIntegracao() {
  const { t } = useI18n();
  const [categorias, setCategorias] = useState<IntegrationCategory[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedCategorias, setSelectedCategorias] = useState<IntegrationCategory[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingCategoria, setEditingCategoria] = useState<IntegrationCategory | null>(null);

  const { execute: fetchCategorias, loading } = useApi<PaginatedResult<IntegrationCategory>>({
    showErrorMessage: true,
  });

  const { execute: deleteCategorias } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('integration.category.list.removed'), variant: 'success' });
    },
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

  const handleEdit = () => {
    if (selectedCategorias.length === 1) {
      setEditingCategoria(selectedCategorias[0]);
      setIsFormOpen(true);
    }
  };

  const hasSystemSelected = selectedCategorias.some((item) => item.isSystem);

  const handleDelete = () => {
    if (hasSystemSelected) {
      toast({
        title: t('integration.category.list.deleteSystemBlockedTitle'),
        description: t('integration.category.list.deleteSystemBlocked'),
        variant: 'destructive',
      });
      return;
    }
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const categoria of selectedCategorias) {
      await deleteCategorias(() => integrationCategoryService.delete(categoria.id));
    }
    setIsConfirmOpen(false);
    setSelectedCategorias([]);
    void loadCategorias();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingCategoria(null);
    setSelectedCategorias([]);
    void loadCategorias();
  };

  const columns: DataTableColumn<IntegrationCategory>[] = [
    {
      key: 'name',
      title: t('common.column.name'),
      dataIndex: 'name',
      sortable: true,
      render: (value: string, record) => (
        <span className="inline-flex items-center gap-2">
          <span className="font-medium">{value}</span>
          {record.isSystem && (
            <Badge variant="secondary" className="text-xs">{t('integration.category.list.systemBadge')}</Badge>
          )}
        </span>
      ),
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
        <Badge variant={value ? 'success' : 'secondary'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('integration.category.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadCategorias()}
      selectedRowsCount={selectedCategorias.length}
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
        selectable
        selectedRows={selectedCategorias}
        onSelectionChange={setSelectedCategorias}
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
        description={t('integration.category.list.deleteDescription').replace('{0}', String(selectedCategorias.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
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
