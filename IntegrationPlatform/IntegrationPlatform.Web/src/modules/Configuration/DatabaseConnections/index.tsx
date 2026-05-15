import { useEffect, useState } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { databaseConnectionService } from '../../../services/databaseConnectionService';
import { DatabaseTypeLabels } from '../../../types/databaseConnection';
import type { DatabaseConnection, DatabaseType } from '../../../types/databaseConnection';
import ConexaoBancoDadosFormModal from '../../../components/modals/DatabaseConnectionFormModal';

const tipoBancoVariantMap: Record<number, string> = {
  0: 'default',
  1: 'secondary',
  2: 'warning',
  3: 'success',
};

export default function ConexoesBancoDados() {
  const { t } = useI18n();
  const [conexoes, setConexoes] = useState<DatabaseConnection[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedConexoes, setSelectedConexoes] = useState<DatabaseConnection[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingConexao, setEditingConexao] = useState<DatabaseConnection | null>(null);

  const { execute: fetchConexoes, loading } = useApi<PaginatedResult<DatabaseConnection>>({
    showErrorMessage: true,
  });

  const { execute: deleteConexoes } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('database.connection.list.removed'), variant: 'success' });
    },
  });

  const loadConexoes = async () => {
    const result = await fetchConexoes(() =>
      databaseConnectionService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      setConexoes(result.data);
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
    void loadConexoes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch]);

  const handleAdd = () => {
    setEditingConexao(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selectedConexoes.length === 1) {
      setEditingConexao(selectedConexoes[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const conexao of selectedConexoes) {
      await deleteConexoes(() => databaseConnectionService.delete(conexao.id));
    }
    setIsConfirmOpen(false);
    setSelectedConexoes([]);
    void loadConexoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingConexao(null);
    setSelectedConexoes([]);
    void loadConexoes();
  };

  const columns: DataTableColumn<DatabaseConnection>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name', sortable: true },
    {
      key: 'type',
      title: t('common.column.type'),
      dataIndex: 'type',
      width: 130,
      render: (value: DatabaseType) => (
        <Badge variant={(tipoBancoVariantMap[value] || 'outline') as 'default' | 'secondary' | 'warning' | 'success'}>
          {DatabaseTypeLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'host', title: t('common.column.host'), dataIndex: 'host', hiddenBelow: 'sm' },
    { key: 'database', title: t('common.column.database'), dataIndex: 'database', hiddenBelow: 'md' },
    { key: 'username', title: t('common.column.username'), dataIndex: 'username', hiddenBelow: 'lg' },
  ];

  return (
    <PageLayout
      title={t('database.connection.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadConexoes()}
      selectedRowsCount={selectedConexoes.length}
    >
      <TableToolbar
        searchValue={search}
        onSearchChange={setSearch}
        searchPlaceholder={t('common.action.search')}
        className="mb-3"
      />

      <DataTable
        data={conexoes}
        columns={columns}
        rowKey="id"
        loading={loading}
        selectable
        selectedRows={selectedConexoes}
        onSelectionChange={setSelectedConexoes}
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

      <ConexaoBancoDadosFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        onSuccess={handleFormSuccess}
        conexao={editingConexao}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('database.connection.list.deleteTitle')}
        description={t('database.connection.list.deleteDescription').replace('{0}', String(selectedConexoes.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
      />
    </PageLayout>
  );
}
