import { useEffect, useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, ConfirmModal, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { referenceService } from '../../../services/referenceService';
import type { Reference } from '../../../types/reference';
import type { Conector } from '../../../types/connector';
import ReferenceFormModal from '../../../components/modals/ReferenceFormModal';
import { formatDateTime } from '../../../utils/formatters';
import { useBulkRun } from '../../../lib/useBulkRun';

export default function References() {
  const { t } = useI18n();
  const [referencias, setReferencias] = useState<Reference[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRows, setSelectedRows] = useState<Reference[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<Reference[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingReferencia, setEditingReferencia] = useState<Reference | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchReferencias, loading } = useApi<PaginatedResult<Reference>>({
    showErrorMessage: true,
  });

  const loadReferencias = async () => {
    const result = await fetchReferencias(() =>
      referenceService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      setReferencias(result.data);
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
    void loadReferencias();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch]);

  const handleAdd = () => {
    setEditingReferencia(null);
    setIsFormOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (referencia) => referenceService.delete(referencia.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadReferencias();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingReferencia(null);
    setSelectedRows([]);
    void loadReferencias();
  };

  const rowActions: DataTableRowAction<Reference>[] = [
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingReferencia(row);
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

  const bulkActions: DataTableBulkAction<Reference>[] = [
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

  const columns: DataTableColumn<Reference>[] = [
    {
      key: 'connector',
      title: t('common.column.connector'),
      dataIndex: 'connector',
      render: (value: Conector) => value?.name || '-',
    },
    {
      key: 'entity',
      title: t('common.column.entity'),
      dataIndex: 'entity',
      hiddenBelow: 'sm',
    },
    {
      key: 'internalId',
      title: t('common.column.internalId'),
      dataIndex: 'internalId',
      hiddenBelow: 'md',
    },
    {
      key: 'externalId',
      title: t('common.column.externalId'),
      dataIndex: 'externalId',
      hiddenBelow: 'md',
    },
    {
      key: 'createdAt',
      title: t('common.column.createdAt'),
      dataIndex: 'createdAt',
      hiddenBelow: 'lg',
      render: (value: string) => formatDateTime(value),
    },
  ];

  return (
    <PageLayout
      title={t('reference.list.title')}
      onAdd={handleAdd}
      onRefresh={() => void loadReferencias()}
    >
      <TableToolbar
        searchValue={search}
        onSearchChange={setSearch}
        searchPlaceholder={t('common.action.search')}
        className="mb-3"
      />

      <DataTable
        columns={columns}
        data={referencias}
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
        title={t('reference.list.deleteTitle')}
        description={t('reference.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
      />

      <ReferenceFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        referencia={editingReferencia}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
