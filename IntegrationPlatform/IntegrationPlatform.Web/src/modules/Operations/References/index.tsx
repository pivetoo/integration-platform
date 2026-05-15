import { useEffect, useState } from 'react';
import { PageLayout, DataTable, ConfirmModal, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { referenceService } from '../../../services/referenceService';
import type { Reference } from '../../../types/reference';
import type { Conector } from '../../../types/connector';
import ReferenceFormModal from '../../../components/modals/ReferenceFormModal';
import { formatDateTime } from '../../../utils/formatters';

export default function References() {
  const { t } = useI18n();
  const [referencias, setReferencias] = useState<Reference[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedReferencias, setSelectedReferencias] = useState<Reference[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingReferencia, setEditingReferencia] = useState<Reference | null>(null);

  const { execute: fetchReferencias, loading } = useApi<PaginatedResult<Reference>>({
    showErrorMessage: true,
  });

  const { execute: deleteReferencias } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('reference.list.removed'), variant: 'success' });
    },
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

  const handleEdit = () => {
    if (selectedReferencias.length === 1) {
      setEditingReferencia(selectedReferencias[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const referencia of selectedReferencias) {
      await deleteReferencias(() => referenceService.delete(referencia.id));
    }
    setIsConfirmOpen(false);
    setSelectedReferencias([]);
    void loadReferencias();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingReferencia(null);
    setSelectedReferencias([]);
    void loadReferencias();
  };

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
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadReferencias()}
      selectedRowsCount={selectedReferencias.length}
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
        selectable
        selectedRows={selectedReferencias}
        onSelectionChange={setSelectedReferencias}
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
        description={t('reference.list.deleteDescription').replace('{0}', String(selectedReferencias.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
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
