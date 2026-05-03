import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { connectorService } from '../../../services/connectorService';
import type { Conector } from '../../../types/connector';
import type { Integration } from '../../../types/integration';
import ConectorFormModal from '../../../components/modals/ConnectorFormModal';

export default function Conectores() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [conectores, setConectores] = useState<Conector[]>([]);
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
    const result = await fetchConectores(() => connectorService.getAll());
    if (result) {
      setConectores(result.data);
    }
  };

  useEffect(() => {
    loadConectores();
  }, []);

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
    loadConectores();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingConector(null);
    setSelectedConectores([]);
    loadConectores();
  };

  const columns: DataTableColumn<Conector>[] = [
    { key: 'name', title: t('common.column.name'), dataIndex: 'name' },
    {
      key: 'integration',
      title: t('common.column.integration'),
      dataIndex: 'integration',
      render: (value: Integration) => value?.name || '-',
    },
    {
      key: 'isActive',
      title: t('common.column.active'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.boolean.yes') : t('common.boolean.no')}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('connector.list.title')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadConectores}
      selectedRowsCount={selectedConectores.length}
    >
      <DataTable
        columns={columns}
        data={conectores}
        rowKey="id"
        selectedRows={selectedConectores}
        onSelectionChange={setSelectedConectores}
        onRowDoubleClick={handleRowDoubleClick}
        emptyText={t('connector.list.empty')}
        loading={loading}
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
