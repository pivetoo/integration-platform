import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { connectorService } from '../../../services/connectorService';
import type { Conector } from '../../../types/connector';
import type { Integration } from '../../../types/integration';
import ConectorFormModal from '../../../components/modals/ConnectorFormModal';
import DetailsButton from '../../../components/DetailsButton';

export default function Conectores() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [conectores, setConectores] = useState<Conector[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedConectores, setSelectedConectores] = useState<Conector[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingConector, setEditingConector] = useState<Conector | null>(null);

  const { execute: fetchConectores, loading } = useApi<PaginatedResult<Conector>>({
    showErrorMessage: true,
  });

  const { execute: deleteConectores } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('connector.list.removed'), variant: 'success' });
    },
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

  const handleEdit = () => {
    if (selectedConectores.length === 1) {
      setEditingConector(selectedConectores[0]);
      setIsFormOpen(true);
    }
  };

  const handleRowDoubleClick = (conector: Conector) => {
    navigate(`/conectores/${conector.id}`);
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const conector of selectedConectores) {
      await deleteConectores(() => connectorService.delete(conector.id));
    }
    setIsConfirmOpen(false);
    setSelectedConectores([]);
    void loadConectores();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingConector(null);
    setSelectedConectores([]);
    void loadConectores();
  };

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
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.boolean.yes') : t('common.boolean.no')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      dataIndex: undefined,
      width: 110,
      render: (_: unknown, record: Conector) => (
        <DetailsButton onClick={(e) => { e.stopPropagation(); navigate(`/conectores/${record.id}`); }} />
      ),
    },
  ];

  return (
    <PageLayout
      title={t('connector.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadConectores()}
      selectedRowsCount={selectedConectores.length}
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
        selectable
        selectedRows={selectedConectores}
        onSelectionChange={setSelectedConectores}
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
        title={t('connector.list.deleteTitle')}
        description={t('connector.list.deleteDescription').replace('{0}', String(selectedConectores.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
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
