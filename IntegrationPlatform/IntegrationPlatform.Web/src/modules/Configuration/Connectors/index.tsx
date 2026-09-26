import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Eye, Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { connectorService } from '../../../services/connectorService';
import type { Conector } from '../../../types/connector';
import type { Integration } from '../../../types/integration';
import ConectorFormModal from '../../../components/modals/ConnectorFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';

export default function Conectores() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [conectores, setConectores] = useState<Conector[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('active');
  const [selectedRows, setSelectedRows] = useState<Conector[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<Conector[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingConector, setEditingConector] = useState<Conector | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchConectores, loading } = useApi<PaginatedResult<Conector>>({
    showErrorMessage: true,
  });

  const loadConectores = async () => {
    const result = await fetchConectores(() =>
      connectorService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      const filtered = result.data.filter((c: Conector) => {
        if (statusFilter === 'active') return c.isActive;
        if (statusFilter === 'inactive') return !c.isActive;
        return true;
      });
      setConectores(filtered);
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
    void loadConectores();
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
    setEditingConector(null);
    setIsFormOpen(true);
  };

  const handleRowClick = (conector: Conector) => {
    navigate(`/conectores/${conector.id}`);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (conector) => connectorService.delete(conector.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadConectores();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingConector(null);
    setSelectedRows([]);
    void loadConectores();
  };

  const rowActions: DataTableRowAction<Conector>[] = [
    {
      key: 'details',
      label: t('common.action.details') !== 'common.action.details' ? t('common.action.details') : 'Detalhes',
      icon: <Eye className="h-4 w-4" />,
      onClick: (row) => navigate(`/conectores/${row.id}`),
    },
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingConector(row);
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

  const bulkActions: DataTableBulkAction<Conector>[] = [
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

  const columns: DataTableColumn<Conector>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name', sortable: true },
    {
      key: 'integration',
      title: t('common.column.integration'),
      dataIndex: 'integration',
      render: (value: Integration) => value?.name || '-',
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge dot variant={value ? 'soft-success' : 'soft-neutral'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('connector.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadConectores()}
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
        data={conectores}
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
        title={t('connector.list.deleteTitle')}
        description={t('connector.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
      />

      <ConectorFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        connector={editingConector}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
