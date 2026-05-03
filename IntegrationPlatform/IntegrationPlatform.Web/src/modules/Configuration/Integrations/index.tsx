import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { Download, Upload } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { integrationService } from '../../../services/integrationService';
import type { Integration, IntegrationExportModel } from '../../../types/integration';
import type { IntegrationCategory } from '../../../types/integrationCategory';
import IntegracaoFormModal from '../../../components/modals/IntegrationFormModal';
import { parseJsonSafe } from '../../../utils/json';

export default function Integracoes() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [integracoes, setIntegracoes] = useState<Integration[]>([]);
  const [selectedIntegracoes, setSelectedIntegracoes] = useState<Integration[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingIntegracao, setEditingIntegracao] = useState<Integration | null>(null);

  const { execute: fetchIntegracoes, loading } = useApi<PaginatedResult<Integration>>({
    showErrorMessage: true,
  });

  const { execute: deleteIntegracoes } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: 'Removido', description: 'Integração removida com sucesso', variant: 'success' });
    },
  });

  const { execute: exportarIntegracao } = useApi<IntegrationExportModel>({
    showErrorMessage: true,
  });

  const { execute: importarIntegracao } = useApi<{ id: number; message: string }>({
    showSuccessMessage: true,
    showErrorMessage: true,
  });

  const fileInputRef = useRef<HTMLInputElement>(null);

  const loadIntegracoes = async () => {
    const result = await fetchIntegracoes(() => integrationService.getAll());
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

  const handleRowDoubleClick = (integracao: Integration) => {
    navigate(`/integracoes/${integracao.id}`);
  };

  const handleDelete = () => {
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    for (const integracao of selectedIntegracoes) {
      await deleteIntegracoes(() => integrationService.delete(integracao.id));
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

    const result = await exportarIntegracao(() => integrationService.export(selectedIntegracoes[0].id));
    if (result) {
      const json = JSON.stringify(result, null, 2);
      const blob = new Blob([json], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${result.identifier}.json`;
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

    const text = await file.text();
    const data = parseJsonSafe<IntegrationExportModel>(text);

    if (!data) {
      toast({ title: 'Erro', description: 'Arquivo JSON inválido', variant: 'destructive' });
    } else {
      await importarIntegracao(() => integrationService.import(data));
      loadIntegracoes();
    }

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const columns: DataTableColumn<Integration>[] = [
    { key: 'identifier', title: t('common.column.identifier'), dataIndex: 'identifier' },
    { key: 'name', title: t('common.column.name'), dataIndex: 'name' },
    {
      key: 'integrationCategory',
      title: t('common.column.category'),
      dataIndex: 'integrationCategory',
      render: (value: IntegrationCategory) => value?.name || '-',
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
