import { useState, useEffect } from 'react';
import { ExternalLink } from 'lucide-react';
import { PageLayout, DataTable, ConfirmModal, useApi, toast } from 'd-rts';
import type { DataTableColumn, PaginatedResult } from 'd-rts';
import { referenciaService } from '../../services/referenciaService';
import type { Referencia } from '../../types/referencia';
import type { Conector } from '../../types/conector';
import ReferenciaFormModal from '../../components/modals/ReferenciaFormModal';

function formatDateTime(dateStr?: string): string {
  if (!dateStr) {
    return '-';
  }
  return new Date(dateStr).toLocaleString('pt-BR');
}

export default function Referencias() {
  const [referencias, setReferencias] = useState<Referencia[]>([]);
  const [selectedReferencias, setSelectedReferencias] = useState<Referencia[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingReferencia, setEditingReferencia] = useState<Referencia | null>(null);

  const { execute: fetchReferencias, loading } = useApi<PaginatedResult<Referencia>>({
    showErrorMessage: true,
  });

  const { execute: deleteReferencias } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: 'Removido', description: 'Referência removida com sucesso', variant: 'success' });
    },
  });

  const loadReferencias = async () => {
    const result = await fetchReferencias(() => referenciaService.getAll());
    if (result) {
      setReferencias(result.data);
    }
  };

  useEffect(() => {
    loadReferencias();
  }, []);

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
      await deleteReferencias(() => referenciaService.delete(referencia.id));
    }
    setIsConfirmOpen(false);
    setSelectedReferencias([]);
    loadReferencias();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingReferencia(null);
    setSelectedReferencias([]);
    loadReferencias();
  };

  const columns: DataTableColumn<Referencia>[] = [
    {
      key: 'conector',
      title: 'Conector',
      dataIndex: 'conector',
      render: (value: Conector) => value?.nome || '-',
    },
    {
      key: 'entidade',
      title: 'Entidade',
      dataIndex: 'entidade',
    },
    {
      key: 'idInterno',
      title: 'ID Interno',
      dataIndex: 'idInterno',
    },
    {
      key: 'idExterno',
      title: 'ID Externo',
      dataIndex: 'idExterno',
    },
    {
      key: 'criadoEm',
      title: 'Criado em',
      dataIndex: 'criadoEm',
      render: (value: string) => formatDateTime(value),
    },
  ];

  return (
    <PageLayout
      title="Referências"
      icon={<ExternalLink size={24} />}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadReferencias}
      selectedRowsCount={selectedReferencias.length}
    >
      <DataTable
        columns={columns}
        data={referencias}
        rowKey="id"
        selectedRows={selectedReferencias}
        onSelectionChange={setSelectedReferencias}
        emptyText="Nenhuma referência encontrada"
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Excluir Referência"
        description={`Tem certeza que deseja excluir ${selectedReferencias.length} referência(s)?`}
        confirmText="Excluir"
        cancelText="Cancelar"
        variant="danger"
      />

      <ReferenciaFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        referencia={editingReferencia}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
