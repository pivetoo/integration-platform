import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { conectorService } from '../../services/conectorService';
import type { Conector } from '../../types/conector';
import type { Integracao } from '../../types/integracao';
import ConectorFormModal from '../../components/modals/ConectorFormModal';

export default function Conectores() {
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
      toast({ title: 'Removido', description: 'Conector removido com sucesso', variant: 'success' });
    },
  });

  const loadConectores = async () => {
    const result = await fetchConectores(() => conectorService.getAll());
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
      await deleteConectores(() => conectorService.delete(conector.id));
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
    { key: 'nome', title: 'Nome', dataIndex: 'nome' },
    {
      key: 'integracao',
      title: 'Integração',
      dataIndex: 'integracao',
      render: (value: Integracao) => value?.nome || '-',
    },
    {
      key: 'ativo',
      title: 'Ativo',
      dataIndex: 'ativo',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Sim' : 'Não'}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title="Conectores"
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
        emptyText="Nenhum conector encontrado"
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Excluir Conector"
        description={`Tem certeza que deseja excluir ${selectedConectores.length} conector(es)?`}
        confirmText="Excluir"
        cancelText="Cancelar"
        variant="danger"
      />

      <ConectorFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        conector={editingConector}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
