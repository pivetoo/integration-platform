import { useEffect, useMemo, useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, ConfirmModal, Badge, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { serviceContractService } from '../../../services/serviceContractService';
import { integrationCategoryService } from '../../../services/integrationCategoryService';
import type { ServiceContract } from '../../../types/serviceContract';
import type { IntegrationCategory } from '../../../types/integrationCategory';
import ServiceContractFormModal from '../../../components/modals/ServiceContractFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';

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
  const [selectedRows, setSelectedRows] = useState<ServiceContract[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<ServiceContract[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editing, setEditing] = useState<ServiceContract | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchContracts, loading } = useApi<PaginatedResult<ServiceContract>>({ showErrorMessage: true });
  const { execute: fetchCategories } = useApi<IntegrationCategory[]>({ showErrorMessage: true });

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

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (contract) => serviceContractService.delete(contract.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadContracts();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditing(null);
    setSelectedRows([]);
    void loadContracts();
  };

  const rowActions: DataTableRowAction<ServiceContract>[] = [
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditing(row);
        setIsFormOpen(true);
      },
    },
    {
      key: 'delete',
      label: t('common.action.delete'),
      icon: <Trash2 className="h-4 w-4" />,
      variant: 'danger',
      hidden: (row) => row.isSystem,
      onClick: (row) => {
        setItemsToDelete([row]);
        setIsConfirmOpen(true);
      },
    },
  ];

  const bulkActions: DataTableBulkAction<ServiceContract>[] = [
    {
      key: 'delete',
      label: t('common.action.delete'),
      icon: <Trash2 className="h-4 w-4" />,
      variant: 'danger',
      disabled: bulkRunning || selectedRows.some((row) => row.isSystem),
      onClick: (rows) => {
        setItemsToDelete(rows);
        setIsConfirmOpen(true);
      },
    },
  ];

  const columns: DataTableColumn<ServiceContract>[] = [
    {
      key: 'name',
      title: t('common.column.name'),
      dataIndex: 'name',
      sortable: true,
      render: (value: string, record) => (
        <span className="inline-flex items-center gap-2">
          <span className="font-medium">{value}</span>
          {record.isSystem && <Badge variant="outline" className="text-xs">{t('serviceContract.list.systemBadge')}</Badge>}
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
          <Badge dot variant="soft-success">{t('serviceContract.callback.yes')}</Badge>
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
        <Badge dot variant={value ? 'soft-success' : 'soft-neutral'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('serviceContract.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadContracts()}
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
        title={t('serviceContract.list.deleteTitle')}
        description={t('serviceContract.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
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
