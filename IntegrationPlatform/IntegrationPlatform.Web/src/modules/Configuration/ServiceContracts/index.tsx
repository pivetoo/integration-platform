import { useEffect, useMemo, useState } from 'react';
import { PageLayout, DataTable, ConfirmModal, Badge, FilterPanel, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { serviceContractService } from '../../../services/serviceContractService';
import { integrationCategoryService } from '../../../services/integrationCategoryService';
import type { ServiceContract } from '../../../types/serviceContract';
import type { IntegrationCategory } from '../../../types/integrationCategory';
import ServiceContractFormModal from '../../../components/modals/ServiceContractFormModal';

export default function ServiceContracts() {
  const { t } = useI18n();
  const [contracts, setContracts] = useState<ServiceContract[]>([]);
  const [categories, setCategories] = useState<IntegrationCategory[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selected, setSelected] = useState<ServiceContract[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editing, setEditing] = useState<ServiceContract | null>(null);

  const { execute: fetchContracts, loading } = useApi<PaginatedResult<ServiceContract>>({ showErrorMessage: true });
  const { execute: fetchCategories } = useApi<IntegrationCategory[]>({ showErrorMessage: true });
  const { execute: deleteContract } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('serviceContract.list.removed'), variant: 'success' });
    },
  });

  const categoryMap = useMemo(() => {
    const map = new Map<number, string>();
    categories.forEach((category) => map.set(category.id, category.name));
    return map;
  }, [categories]);

  useEffect(() => {
    void fetchCategories(() => integrationCategoryService.getActive()).then((result) => {
      if (result) {
        setCategories(result);
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadContracts = async () => {
    const result = await fetchContracts(() =>
      serviceContractService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        integrationCategoryId: categoryFilter ? Number(categoryFilter) : undefined,
      }),
    );
    if (result) {
      const filtered = result.data.filter((contract: ServiceContract) => {
        if (statusFilter === 'active') return contract.isActive;
        if (statusFilter === 'inactive') return !contract.isActive;
        return true;
      });
      setContracts(filtered);
      setTotalCount(result.total ?? 0);
    }
  };

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timeout);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, categoryFilter, statusFilter]);

  useEffect(() => {
    void loadContracts();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, categoryFilter, statusFilter]);

  const filterSections: FilterSection[] = useMemo(
    () => [
      {
        key: 'category',
        label: t('serviceContract.column.category'),
        value: categoryFilter,
        onChange: setCategoryFilter,
        options: categories.map((category) => ({ value: String(category.id), label: category.name })),
        allLabel: t('common.filter.all'),
      },
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
    [categories, categoryFilter, statusFilter, t],
  );

  const clearFilters = () => {
    setCategoryFilter('');
    setStatusFilter('');
  };

  const handleAdd = () => {
    setEditing(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selected.length === 1) {
      setEditing(selected[0]);
      setIsFormOpen(true);
    }
  };

  const hasSystemSelected = selected.some((item) => item.isSystem);

  const handleDelete = () => {
    if (hasSystemSelected) {
      toast({
        title: t('serviceContract.list.deleteSystemBlockedTitle'),
        description: t('serviceContract.list.deleteSystemBlocked'),
        variant: 'destructive',
      });
      return;
    }
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const contract of selected) {
      await deleteContract(() => serviceContractService.delete(contract.id));
    }
    setIsConfirmOpen(false);
    setSelected([]);
    void loadContracts();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditing(null);
    setSelected([]);
    void loadContracts();
  };

  const columns: DataTableColumn<ServiceContract>[] = [
    {
      key: 'name',
      title: t('common.column.name'),
      dataIndex: 'name',
      sortable: true,
      render: (value: string, record) => (
        <span className="inline-flex items-center gap-2">
          <span className="font-medium">{value}</span>
          {record.isSystem && <Badge variant="secondary" className="text-xs">{t('serviceContract.list.systemBadge')}</Badge>}
        </span>
      ),
    },
    {
      key: 'identifier',
      title: t('serviceContract.column.identifier'),
      dataIndex: 'identifier',
      width: 240,
      render: (value: string) => <code className="rounded bg-muted px-1.5 py-0.5 text-xs">{value}</code>,
    },
    {
      key: 'category',
      title: t('serviceContract.column.category'),
      dataIndex: 'integrationCategoryId',
      hiddenBelow: 'md',
      render: (value: number) => categoryMap.get(value) ?? '-',
    },
    {
      key: 'hasCallback',
      title: t('serviceContract.column.callback'),
      dataIndex: 'hasCallback',
      width: 110,
      hiddenBelow: 'lg',
      render: (value: boolean) =>
        value ? (
          <Badge variant="success">{t('serviceContract.callback.yes')}</Badge>
        ) : (
          <span className="text-xs text-muted-foreground">{t('serviceContract.callback.no')}</span>
        ),
    },
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
      title={t('serviceContract.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadContracts()}
      selectedRowsCount={selected.length}
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
        data={contracts}
        rowKey="id"
        loading={loading}
        selectable
        selectedRows={selected}
        onSelectionChange={setSelected}
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
        title={t('serviceContract.list.deleteTitle')}
        description={t('serviceContract.list.deleteDescription').replace('{0}', String(selected.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <ServiceContractFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        serviceContract={editing}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
