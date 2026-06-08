import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Download, Upload } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { integrationService } from '../../../services/integrationService';
import { integrationCategoryService } from '../../../services/integrationCategoryService';
import type { Integration, IntegrationExportModel } from '../../../types/integration';
import type { IntegrationCategory } from '../../../types/integrationCategory';
import IntegracaoFormModal from '../../../components/modals/IntegrationFormModal';
import DetailsButton from '../../../components/DetailsButton';
import { parseJsonSafe } from '../../../utils/json';

export default function Integracoes() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [integracoes, setIntegracoes] = useState<Integration[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [categoryFilter, setCategoryFilter] = useState<string>('');
  const [categories, setCategories] = useState<IntegrationCategory[]>([]);
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

  useEffect(() => {
    integrationCategoryService.getActive().then(setCategories);
  }, []);

  const loadIntegracoes = async () => {
    const result = await fetchIntegracoes(() =>
      integrationService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
      }),
    );
    if (result) {
      const filtered = result.data.filter((i: Integration) => {
        const statusOk = statusFilter === 'active' ? i.isActive : statusFilter === 'inactive' ? !i.isActive : true;
        const categoryOk = !categoryFilter || String(i.integrationCategoryId) === categoryFilter;
        return statusOk && categoryOk;
      });
      setIntegracoes(filtered);
      setTotalCount(result.total ?? 0);
    }
  };

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timeout);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter, categoryFilter]);

  useEffect(() => {
    void loadIntegracoes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, statusFilter, categoryFilter]);

  const filterSections: FilterSection[] = useMemo(
    () => [
      {
        key: 'status',
        label: t('common.column.status'),
        value: statusFilter,
        onChange: setStatusFilter,
        options: [
          { value: 'active', label: t('common.filter.activeOnly') },
          { value: 'inactive', label: t('common.filter.inactiveOnly') },
        ],
        allLabel: t('common.filter.all'),
      },
      {
        key: 'category',
        label: t('common.column.category'),
        value: categoryFilter,
        onChange: setCategoryFilter,
        options: categories.map(c => ({ value: String(c.id), label: c.name })),
        allLabel: t('common.filter.all'),
      },
    ],
    [statusFilter, categoryFilter, categories, t],
  );

  const clearFilters = () => { setStatusFilter(''); setCategoryFilter(''); };

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
    void loadIntegracoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingIntegracao(null);
    setSelectedIntegracoes([]);
    void loadIntegracoes();
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
      void loadIntegracoes();
    }

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const columns: DataTableColumn<Integration>[] = [
    {
      key: 'iconUrl',
      title: '',
      dataIndex: 'iconUrl',
      width: 60,
      render: (value: string | undefined) => value ? (
        <img
          src={value}
          alt=""
          className="h-7 w-7 rounded-md border bg-card object-contain p-0.5"
          onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
        />
      ) : (
        <div className="h-7 w-7 rounded-md border border-dashed bg-muted/30" />
      ),
    },
    { key: 'identifier', title: t('common.column.identifier'), dataIndex: 'identifier', hiddenBelow: 'md' },
    { key: 'name', title: t('common.column.name'), dataIndex: 'name', sortable: true },
    {
      key: 'integrationCategory',
      title: t('common.column.category'),
      dataIndex: 'integrationCategory',
      hiddenBelow: 'sm',
      render: (value: IntegrationCategory) => value?.name || '-',
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      width: 110,
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      dataIndex: undefined,
      width: 110,
      render: (_: unknown, record: Integration) => (
        <DetailsButton onClick={(e) => { e.stopPropagation(); navigate(`/integracoes/${record.id}`); }} />
      ),
    },
  ];

  return (
    <PageLayout
      title={t("integration.integrations.title")}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onDelete={handleDelete}
      onRefresh={() => void loadIntegracoes()}
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

      <TableToolbar
        searchValue={search}
        onSearchChange={setSearch}
        searchPlaceholder={t('common.action.search')}
        rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
        className="mb-3"
      />

      <DataTable
        columns={columns}
        data={integracoes}
        rowKey="id"
        loading={loading}
        selectable
        selectedRows={selectedIntegracoes}
        onSelectionChange={setSelectedIntegracoes}
        onRowDoubleClick={handleRowDoubleClick}
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
