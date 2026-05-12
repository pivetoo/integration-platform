import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
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
    const result = await fetchConexoes(() => databaseConnectionService.getAll({ pageSize: 500 }));
    if (result) {
      setConexoes(result.data);
    }
  };

  useEffect(() => {
    loadConexoes();
  }, []);

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
    loadConexoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingConexao(null);
    setSelectedConexoes([]);
    loadConexoes();
  };

  const columns: DataTableColumn<DatabaseConnection>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name' },
    {
      key: 'type',
      title: t('common.column.type'),
      dataIndex: 'type',
      render: (value: DatabaseType) => (
        <Badge variant={(tipoBancoVariantMap[value] || 'outline') as 'default' | 'secondary' | 'warning' | 'success'}>
          {DatabaseTypeLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'host', title: t('common.column.host'), dataIndex: 'host' },
    { key: 'database', title: t('common.column.database'), dataIndex: 'database' },
    { key: 'username', title: t('common.column.username'), dataIndex: 'username' },
  ];

  return (
    <PageLayout
      title={t('database.connection.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadConexoes}
      selectedRowsCount={selectedConexoes.length}
    >
      <DataTable
        data={conexoes}
        columns={columns}
        rowKey="id"
        loading={loading}
        selectable
        selectedRows={selectedConexoes}
        onSelectionChange={setSelectedConexoes}
        emptyText={t('database.connection.list.empty')}
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
