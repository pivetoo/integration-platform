import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { conexaoBancoDadosService } from '../../services/conexaoBancoDadosService';
import { TipoBancoDadosLabels } from '../../types/conexaoBancoDados';
import type { ConexaoBancoDados, TipoBancoDados } from '../../types/conexaoBancoDados';
import ConexaoBancoDadosFormModal from '../../components/modals/ConexaoBancoDadosFormModal';

const tipoBancoVariantMap: Record<number, string> = {
  0: 'default',
  1: 'secondary',
  2: 'warning',
  3: 'success',
};

export default function ConexoesBancoDados() {
  const [conexoes, setConexoes] = useState<ConexaoBancoDados[]>([]);
  const [selectedConexoes, setSelectedConexoes] = useState<ConexaoBancoDados[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingConexao, setEditingConexao] = useState<ConexaoBancoDados | null>(null);

  const { execute: fetchConexoes, loading } = useApi<PaginatedResult<ConexaoBancoDados>>({
    showErrorMessage: true,
  });

  const { execute: deleteConexoes } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: 'Removido', description: 'Conexão removida com sucesso', variant: 'success' });
    },
  });

  const loadConexoes = async () => {
    const result = await fetchConexoes(() => conexaoBancoDadosService.getAll());
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
      await deleteConexoes(() => conexaoBancoDadosService.delete(conexao.id));
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

  const columns: DataTableColumn<ConexaoBancoDados>[] = [
    { key: 'nome', title: 'Nome', dataIndex: 'nome' },
    {
      key: 'tipo',
      title: 'Tipo',
      dataIndex: 'tipo',
      render: (value: TipoBancoDados) => (
        <Badge variant={(tipoBancoVariantMap[value] || 'outline') as 'default' | 'secondary' | 'warning' | 'success'}>
          {TipoBancoDadosLabels[value] || '-'}
        </Badge>
      ),
    },
    { key: 'host', title: 'Host', dataIndex: 'host' },
    { key: 'database', title: 'Database', dataIndex: 'database' },
    { key: 'username', title: 'Username', dataIndex: 'username' },
  ];

  return (
    <PageLayout
      title="Conexões de Banco de Dados"
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
        title="Confirmar exclusão"
        description={`Deseja excluir ${selectedConexoes.length} conexão(ões)?`}
      />
    </PageLayout>
  );
}
