import { useEffect, useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { databaseConnectionService } from '../../../services/databaseConnectionService';
import { DatabaseTypeLabels, DatabaseType } from '../../../types/databaseConnection';
import type { DatabaseConnection } from '../../../types/databaseConnection';
import ConexaoBancoDadosFormModal from '../../../components/modals/DatabaseConnectionFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';

const tipoBancoVariantMap: Record<number, string> = {
  [DatabaseType.PostgreSQL]: 'default',
  [DatabaseType.SqlServer]: 'secondary',
  [DatabaseType.Oracle]: 'warning',
  [DatabaseType.MySQL]: 'success',
};

export default function ConexoesBancoDados() {
  const { t } = useI18n();
  const [conexoes, setConexoes] = useState<DatabaseConnection[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRows, setSelectedRows] = useState<DatabaseConnection[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<DatabaseConnection[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingConexao, setEditingConexao] = useState<DatabaseConnection | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchConexoes, loading } = useApi<PaginatedResult<DatabaseConnection>>({
    showErrorMessage: true,
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

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (conexao) => databaseConnectionService.delete(conexao.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadConexoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingConexao(null);
    setSelectedRows([]);
    void loadConexoes();
  };

  const rowActions: DataTableRowAction<DatabaseConnection>[] = [
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingConexao(row);
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

  const bulkActions: DataTableBulkAction<DatabaseConnection>[] = [
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
      onRefresh={() => void loadConexoes()}
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
        description={t('database.connection.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
      />
    </PageLayout>
  );
}
