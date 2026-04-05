import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { Download, Upload } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../types/pagination';
import { integracaoService } from '../../services/integracaoService';
import type { Integracao, IntegracaoExportModel } from '../../types/integracao';
import type { CategoriaIntegracao } from '../../types/categoriaIntegracao';
import IntegracaoFormModal from '../../components/modals/IntegracaoFormModal';

export default function Integracoes() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [integracoes, setIntegracoes] = useState<Integracao[]>([]);
  const [selectedIntegracoes, setSelectedIntegracoes] = useState<Integracao[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingIntegracao, setEditingIntegracao] = useState<Integracao | null>(null);

  const { execute: fetchIntegracoes, loading } = useApi<PaginatedResult<Integracao>>({
    showErrorMessage: true,
  });

  const { execute: deleteIntegracoes } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: 'Removido', description: 'Integração removida com sucesso', variant: 'success' });
    },
  });

  const { execute: exportarIntegracao } = useApi<IntegracaoExportModel>({
    showErrorMessage: true,
  });

  const { execute: importarIntegracao } = useApi<{ id: number; message: string }>({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const fileInputRef = useRef<HTMLInputElement>(null);

  const loadIntegracoes = async () => {
    const result = await fetchIntegracoes(() => integracaoService.getAll());
    if (result) {
      setIntegracoes(result.data);
    }
  };

  useEffect(() => {
    loadIntegracoes();
  }, []);

  const handleAdd = () => {
    setEditingIntegracao(null);
    setIsFormOpen(true);
  };

  const handleEdit = () => {
    if (selectedIntegracoes.length === 1) {
      setEditingIntegracao(selectedIntegracoes[0]);
      setIsFormOpen(true);
    }
  };

  const handleRowDoubleClick = (integracao: Integracao) => {
    navigate(`/integracoes/${integracao.id}`);
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const integracao of selectedIntegracoes) {
      await deleteIntegracoes(() => integracaoService.delete(integracao.id));
    }
    setIsConfirmOpen(false);
    setSelectedIntegracoes([]);
    loadIntegracoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingIntegracao(null);
    setSelectedIntegracoes([]);
    loadIntegracoes();
  };

  const handleExport = async () => {
    if (selectedIntegracoes.length !== 1) {
      return;
    }

    const result = await exportarIntegracao(() => integracaoService.exportar(selectedIntegracoes[0].id));
    if (result) {
      const json = JSON.stringify(result, null, 2);
      const blob = new Blob([json], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${result.identificador}.json`;
      link.click();
      URL.revokeObjectURL(url);
      toast({ title: 'Sucesso', description: 'Integração exportada com sucesso', variant: 'success' });
    }
  };

  const handleImportClick = () => {
    fileInputRef.current?.click();
  };

  const handleImportFile = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    try {
      const text = await file.text();
      const data: IntegracaoExportModel = JSON.parse(text);
      await importarIntegracao(() => integracaoService.importar(data));
      loadIntegracoes();
    } catch {
      toast({ title: 'Erro', description: 'Arquivo JSON inválido', variant: 'destructive' });
    }

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const columns: DataTableColumn<Integracao>[] = [
    { key: 'identificador', title: t('common.column.identifier'), dataIndex: 'identificador' },
    { key: 'nome', title: t('common.column.name'), dataIndex: 'nome' },
    {
      key: 'categoria',
      title: t('common.column.category'),
      dataIndex: 'categoria',
      render: (value: CategoriaIntegracao) => value?.nome || '-',
    },
    {
      key: 'ativo',
      title: t('common.column.active'),
      dataIndex: 'ativo',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.boolean.yes') : t('common.boolean.no')}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title={t("integration.integrations.title")}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={loadIntegracoes}
      selectedRowsCount={selectedIntegracoes.length}
      actions={[
        {
          key: 'exportar',
          label: t('common.action.export'),
          icon: <Download size={16} />,
          variant: 'outline',
          onClick: handleExport,
          disabled: selectedIntegracoes.length !== 1,
        },
        {
          key: 'importar',
          label: t('common.action.import'),
          icon: <Upload size={16} />,
          variant: 'outline',
          onClick: handleImportClick,
        },
      ]}
    >
      <input
        ref={fileInputRef}
        type="file"
        accept=".json"
        onChange={handleImportFile}
        style={{ display: 'none' }}
      />
      <DataTable
        columns={columns}
        data={integracoes}
        rowKey="id"
        selectedRows={selectedIntegracoes}
        onSelectionChange={setSelectedIntegracoes}
        onRowDoubleClick={handleRowDoubleClick}
        emptyText={t("integration.integrations.empty")}
        loading={loading}
      />

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('integration.list.deleteTitle')}
        description={t('integration.list.deleteDescription').replace('{0}', String(selectedIntegracoes.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <IntegracaoFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        integracao={editingIntegracao}
        onSuccess={handleFormSuccess}
      />
    </PageLayout>
  );
}
