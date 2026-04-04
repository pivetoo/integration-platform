import { useState, useEffect } from 'react';
import { PageLayout, DataTable, ConfirmModal, useApi, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { scriptBancoDadosService } from '../../services/scriptBancoDadosService';
import type { ScriptBancoDados } from '../../types/scriptBancoDados';
import type { ConexaoBancoDados } from '../../types/conexaoBancoDados';
import ScriptBancoDadosFormModal from '../../components/modals/ScriptBancoDadosFormModal';

export default function ScriptsBancoDados() {
  const [scripts, setScripts] = useState<ScriptBancoDados[]>([]);
  const [selectedScripts, setSelectedScripts] = useState<ScriptBancoDados[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingScript, setEditingScript] = useState<ScriptBancoDados | null>(null);

  const { execute: fetchScripts, loading } = useApi<PaginatedResult<ScriptBancoDados>>({
    showErrorMessage: true,
  });

  const { execute: deleteScripts } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: 'Removido', description: 'Script removido com sucesso', variant: 'success' });
    },
  });

  const loadScripts = async () => {
    const result = await fetchScripts(() => scriptBancoDadosService.getAll());
    if (result) {
      setScripts(result.data);
    }
  };

  useEffect(() => {
    loadScripts();
  }, []);

  const handleAdd = () => {
    setEditingScript(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selectedScripts.length === 1) {
      setEditingScript(selectedScripts[0]);
      setIsFormOpen(true);
    }
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const script of selectedScripts) {
      await deleteScripts(() => scriptBancoDadosService.delete(script.id));
    }
    setIsConfirmOpen(false);
    setSelectedScripts([]);
    loadScripts();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingScript(null);
    setSelectedScripts([]);
    loadScripts();
  };

  const columns: DataTableColumn<ScriptBancoDados>[] = [
    { key: 'nome', title: 'Nome', dataIndex: 'nome' },
    {
      key: 'conexaoBancoDados',
      title: 'Conexão',
      dataIndex: 'conexaoBancoDados',
      render: (value: ConexaoBancoDados) => value?.nome || '-',
    },
    { key: 'descricao', title: 'Descrição', dataIndex: 'descricao' },
  ];

  return (
    <PageLayout
      title="Scripts SQL"
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadScripts}
      selectedRowsCount={selectedScripts.length}
    >
      <DataTable
        columns={columns}
        data={scripts}
        rowKey="id"
        selectedRows={selectedScripts}
        onSelectionChange={setSelectedScripts}
        emptyText="Nenhum script de banco de dados encontrado"
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Excluir Script"
        description={`Tem certeza que deseja excluir ${selectedScripts.length} script(s)?`}
        confirmText="Excluir"
        cancelText="Cancelar"
        variant="danger"
      />

      <ScriptBancoDadosFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        scriptBancoDados={editingScript}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
