import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { Plus, Pencil, Trash2, Cable } from 'lucide-react';
import { PageLayout, Badge, Button, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import { integracaoService } from '../../services/integracaoService';
import { integracaoAtributoService } from '../../services/integracaoAtributoService';
import type { Integracao } from '../../types/integracao';
import { TipoCampoLabels } from '../../types/integracaoAtributo';
import type { IntegracaoAtributo } from '../../types/integracaoAtributo';
import IntegracaoFormModal from '../../components/modals/IntegracaoFormModal';
import IntegracaoAtributoFormModal from '../../components/modals/IntegracaoAtributoFormModal';

export default function IntegracaoDetalhe() {
  const { t } = useI18n();
  const { id } = useParams<{ id: string }>();
  const [integracao, setIntegracao] = useState<Integracao | null>(null);
  const [atributos, setAtributos] = useState<IntegracaoAtributo[]>([]);
  const [isEditIntegracaoOpen, setIsEditIntegracaoOpen] = useState(false);
  const [isAtributoFormOpen, setIsAtributoFormOpen] = useState(false);
  const [editingAtributo, setEditingAtributo] = useState<IntegracaoAtributo | null>(null);
  const [deletingAtributo, setDeletingAtributo] = useState<IntegracaoAtributo | null>(null);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);

  const { execute: fetchIntegracao } = useApi<Integracao>({
    showErrorMessage: true,
  });

  const { execute: fetchAtributos } = useApi<IntegracaoAtributo[]>({
    showErrorMessage: true,
  });

  const { execute: deleteAtributo } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('integration.detail.attributeRemoved'), variant: 'success' });
    },
  });

  const integracaoId = parseInt(id || '0');

  const loadIntegracao = async () => {
    const result = await fetchIntegracao(() => integracaoService.getById(integracaoId));
    if (result) {
      setIntegracao(result);
    }
  };

  const loadAtributos = async () => {
    const result = await fetchAtributos(() => integracaoAtributoService.getByIntegracao(integracaoId));
    if (result) {
      const sorted = [...result].sort((a, b) => a.ordem - b.ordem);
      setAtributos(sorted);
    }
  };

  useEffect(() => {
    if (integracaoId) {
      loadIntegracao();
      loadAtributos();
    }
  }, [integracaoId]);

  const handleAddAtributo = () => {
    setEditingAtributo(null);
    setIsAtributoFormOpen(true);
  };

  const handleEditAtributo = (atributo: IntegracaoAtributo) => {
    setEditingAtributo(atributo);
    setIsAtributoFormOpen(true);
  };

  const handleDeleteAtributo = (atributo: IntegracaoAtributo) => {
    setDeletingAtributo(atributo);
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (deletingAtributo) {
      await deleteAtributo(() => integracaoAtributoService.delete(deletingAtributo.id));
      setIsConfirmOpen(false);
      setDeletingAtributo(null);
      loadAtributos();
    }
  };

  const handleAtributoFormSuccess = () => {
    setIsAtributoFormOpen(false);
    setEditingAtributo(null);
    loadAtributos();
  };

  const handleIntegracaoEditSuccess = () => {
    setIsEditIntegracaoOpen(false);
    loadIntegracao();
  };

  const nextOrdem = atributos.length > 0 ? Math.max(...atributos.map(a => a.ordem)) + 1 : 1;

  return (
    <PageLayout
      title={integracao?.nome || t('integration.detail.fallbackTitle')}
      onRefresh={() => { loadIntegracao(); loadAtributos(); }}
      actions={[
        {
          key: 'edit-integracao',
          label: t('integration.detail.editAction'),
          icon: <Pencil size={16} />,
          variant: 'secondary',
          onClick: () => setIsEditIntegracaoOpen(true),
        },
      ]}
    >
      <div className="space-y-6">

        {integracao && (
          <div className="grid grid-cols-2 gap-4 rounded-lg border bg-card p-4 md:grid-cols-4">
            <div>
              <span className="text-xs text-muted-foreground">{t('common.column.identifier')}</span>
              <p className="font-medium">{integracao.identificador}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">{t('common.column.name')}</span>
              <p className="font-medium">{integracao.nome}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">{t('common.column.category')}</span>
              <p className="font-medium">{integracao.categoria?.nome || '-'}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">{t('common.column.status')}</span>
              <div className="mt-1">
                <Badge variant={integracao.ativo ? 'success' : 'destructive'}>
                  {integracao.ativo ? t('common.status.active') : t('common.status.inactive')}
                </Badge>
              </div>
            </div>
          </div>
        )}

        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">{t('integration.detail.attributesTitle')}</h2>
            <Button size="sm" onClick={handleAddAtributo}>
              <Plus size={16} className="mr-2" />
              {t('integration.detail.newAttribute')}
            </Button>
          </div>

          {atributos.length === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-12 text-muted-foreground">
              <Cable size={48} className="mb-4 opacity-50" />
              <p>{t('integration.detail.emptyTitle')}</p>
              <p className="text-sm">{t('integration.detail.emptyDescription')}</p>
            </div>
          ) : (
            <div className="space-y-2">
              {atributos.map((atributo) => (
                <div
                  key={atributo.id}
                  className="flex items-center gap-3 rounded-lg border bg-card p-4"
                >
                  <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
                    {atributo.ordem}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-medium truncate">{atributo.label}</span>
                      <Badge variant="outline">{TipoCampoLabels[atributo.tipo]}</Badge>
                      {atributo.obrigatorio && (
                        <Badge variant="destructive">{t('integration.detail.required')}</Badge>
                      )}
                      {atributo.sensivel && (
                        <Badge variant="secondary">{t('integration.detail.sensitive')}</Badge>
                      )}
                    </div>
                    <div className="flex items-center gap-3 mt-1 text-sm text-muted-foreground">
                      <span>{t('integration.detail.fieldLabel')}: {atributo.campo}</span>
                      {atributo.grupo && (
                        <>
                          <span>|</span>
                          <span>{t('integration.detail.groupLabel')}: {atributo.grupo}</span>
                        </>
                      )}
                      {atributo.valorPadrao && (
                        <>
                          <span>|</span>
                          <span>{t('integration.detail.defaultLabel')}: {atributo.valorPadrao}</span>
                        </>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center gap-1">
                    <Button variant="ghost" size="sm" onClick={() => handleEditAtributo(atributo)}>
                      <Pencil size={16} />
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => handleDeleteAtributo(atributo)}>
                      <Trash2 size={16} className="text-destructive" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title={t('integration.detail.deleteAttributeTitle')}
        description={t('integration.detail.deleteAttributeDescription').replace('{0}', deletingAtributo?.label || '')}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      <IntegracaoFormModal
        open={isEditIntegracaoOpen}
        onOpenChange={setIsEditIntegracaoOpen}
        integracao={integracao}
        onSuccess={handleIntegracaoEditSuccess}
      />

      {integracaoId > 0 && (
        <IntegracaoAtributoFormModal
          open={isAtributoFormOpen}
          onOpenChange={setIsAtributoFormOpen}
          integracaoId={integracaoId}
          atributo={editingAtributo}
          nextOrdem={nextOrdem}
          onSuccess={handleAtributoFormSuccess}
        />
      )}
    </PageLayout>
  );
}
