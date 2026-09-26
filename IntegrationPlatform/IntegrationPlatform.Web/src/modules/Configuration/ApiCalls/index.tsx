import { useEffect, useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { apiCallService } from '../../../services/apiCallService';
import { HttpMethodLabels, HttpMethod } from '../../../types/apiCall';
import type { ApiCall } from '../../../types/apiCall';
import ChamadaApiFormModal from '../../../components/modals/ApiCallFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';

const metodoVariantMap: Record<number, string> = {
  [HttpMethod.GET]: 'success',
  [HttpMethod.POST]: 'default',
  [HttpMethod.PUT]: 'warning',
  [HttpMethod.PATCH]: 'secondary',
  [HttpMethod.DELETE]: 'destructive',
};

export default function ChamadasApi() {
  const { t } = useI18n();
  const [chamadas, setChamadas] = useState<ApiCall[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRows, setSelectedRows] = useState<ApiCall[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<ApiCall[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingChamada, setEditingChamada] = useState<ApiCall | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchChamadas, loading } = useApi<PaginatedResult<ApiCall>>({
    showErrorMessage: true,
  });
  const { execute: fetchApiCallById } = useApi<ApiCall>({
    showErrorMessage: false,
  });

  const loadChamadas = async () => {
    const result = await fetchChamadas(() =>
      apiCallService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      setChamadas(result.data);
      setTotalCount(result.total ?? 0);
    }
  };

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timeout);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  useEffect(() => {
    void loadChamadas();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch]);

  const handleAdd = () => {
    setEditingChamada(null);
    setIsFormOpen(true);
  };

  const handleEditRow = async (row: ApiCall) => {
    const result = await fetchApiCallById(() => apiCallService.getById(row.id));
    setEditingChamada(result ?? row);
    setIsFormOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (chamada) => apiCallService.delete(chamada.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadChamadas();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingChamada(null);
    setSelectedRows([]);
    void loadChamadas();
  };

  const rowActions: DataTableRowAction<ApiCall>[] = [
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        void handleEditRow(row);
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

  const bulkActions: DataTableBulkAction<ApiCall>[] = [
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

  const columns: DataTableColumn<ApiCall>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name', sortable: true },
    {
      key: 'method',
      title: t('common.column.method'),
      dataIndex: 'method',
      width: 120,
      render: (value: HttpMethod) => (
        <Badge variant={(metodoVariantMap[value] || 'outline') as 'success' | 'default' | 'warning' | 'secondary' | 'destructive'}>
          {HttpMethodLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'url', title: t('common.column.url'), dataIndex: 'url', hiddenBelow: 'md' },
  ];

  return (
    <PageLayout
      title={t('apiCall.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadChamadas()}
    >
      <TableToolbar
        searchValue={search}
        onSearchChange={setSearch}
        searchPlaceholder={t('common.action.search')}
        className="mb-3"
      />

      <DataTable
        columns={columns}
        data={chamadas}
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
        title={t('apiCall.list.deleteTitle')}
        description={t('apiCall.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
      />

      <ChamadaApiFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        chamadaApi={editingChamada}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
