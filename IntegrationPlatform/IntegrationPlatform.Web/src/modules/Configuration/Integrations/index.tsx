import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Download, Upload, Eye, Pencil, Trash2 } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, useApi, useI18n, toast } from 'archon-ui';
import type { DataTableColumn, FilterSection, DataTableRowAction, DataTableBulkAction } from 'archon-ui';
import type { PaginatedResult } from '../../../types/pagination';
import { integrationService } from '../../../services/integrationService';
import { integrationCategoryService } from '../../../services/integrationCategoryService';
import type { Integration, IntegrationExportModel } from '../../../types/integration';
import type { IntegrationCategory } from '../../../types/integrationCategory';
import IntegracaoFormModal from '../../../components/modals/IntegrationFormModal';
import { useBulkRun } from '../../../lib/useBulkRun';
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
  const [selectedRows, setSelectedRows] = useState<Integration[]>([]);
  const [itemsToDelete, setItemsToDelete] = useState<Integration[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingIntegracao, setEditingIntegracao] = useState<Integration | null>(null);
  const { run: runBulk, running: bulkRunning } = useBulkRun();

  const { execute: fetchIntegracoes, loading } = useApi<PaginatedResult<Integration>>({
    showErrorMessage: true,
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
        categoryId: categoryFilter ? Number(categoryFilter) : undefined,
        isActive: statusFilter === 'active' ? true : statusFilter === 'inactive' ? false : undefined,
      }),
    );
    if (result) {
      setIntegracoes(result.data);
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

  const handleRowDoubleClick = (integracao: Integration) => {
    navigate(`/integracoes/${integracao.id}`);
  };

  const handleDeleteConfirm = async () => {
    await runBulk(itemsToDelete, (item) => integrationService.delete(item.id), 'deleted');
    setIsConfirmOpen(false);
    setItemsToDelete([]);
    setSelectedRows([]);
    void loadIntegracoes();
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingIntegracao(null);
    setSelectedRows([]);
    void loadIntegracoes();
  };

  const handleExportItem = async (item: Integration) => {
    const result = await exportarIntegracao(() => integrationService.export(item.id));
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

  const rowActions: DataTableRowAction<Integration>[] = [
    {
      key: 'details',
      label: t('common.action.details') !== 'common.action.details' ? t('common.action.details') : 'Detalhes',
      icon: <Eye className="h-4 w-4" />,
      onClick: (row) => navigate(`/integracoes/${row.id}`),
    },
    {
      key: 'edit',
      label: t('common.action.edit'),
      icon: <Pencil className="h-4 w-4" />,
      onClick: (row) => {
        setEditingIntegracao(row);
        setIsFormOpen(true);
      },
    },
    {
      key: 'export',
      label: t('common.action.export'),
      icon: <Download className="h-4 w-4" />,
      onClick: (row) => void handleExportItem(row),
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

  const bulkActions: DataTableBulkAction<Integration>[] = [
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
        <Badge dot variant={value ? 'soft-success' : 'soft-neutral'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
  ];

  return (
    <PageLayout
      title={t("integration.integrations.title")}
      onAdd={handleAdd}
      onRefresh={() => void loadIntegracoes()}
      actions={[
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
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        rowActions={rowActions}
        bulkActions={bulkActions}
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
        description={t('integration.list.deleteDescription').replace('{0}', String(itemsToDelete.length))}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
        loading={bulkRunning}
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
