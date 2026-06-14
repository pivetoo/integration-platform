import { useEffect, useState } from 'react';
import { Plus, Trash2, ScrollText } from 'lucide-react';
import { Badge, Button, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import { serviceContractService } from '../../../services/serviceContractService';
import type { ServiceContract } from '../../../types/serviceContract';

interface ServiceContractsSectionProps {
  integrationId: number;
  integrationCategoryId?: number;
}

export default function ServiceContractsSection({ integrationId, integrationCategoryId }: ServiceContractsSectionProps) {
  const { t } = useI18n();
  const [bound, setBound] = useState<ServiceContract[]>([]);
  const [available, setAvailable] = useState<ServiceContract[]>([]);
  const [selectedId, setSelectedId] = useState<string>('');
  const [unbinding, setUnbinding] = useState<ServiceContract | null>(null);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);

  const { execute: fetchBound } = useApi<ServiceContract[]>({ showErrorMessage: true });
  const { execute: fetchAll } = useApi<ServiceContract[]>({ showErrorMessage: true });
  const { execute: runBind, loading: binding } = useApi({ showSuccessMessage: false, showErrorMessage: true });
  const { execute: runUnbind } = useApi({ showSuccessMessage: false, showErrorMessage: true });

  const load = async () => {
    const boundResult = await fetchBound(() => serviceContractService.getByIntegration(integrationId));
    const allResult = await fetchAll(() => serviceContractService.getActive());
    const boundList = boundResult ?? [];
    const boundIds = new Set(boundList.map((contract: ServiceContract) => contract.id));
    setBound(boundList);
    setAvailable(
      (allResult ?? []).filter(
        (contract: ServiceContract) => !boundIds.has(contract.id) && (!integrationCategoryId || contract.integrationCategoryId === integrationCategoryId),
      ),
    );
  };

  useEffect(() => {
    if (integrationId) {
      void load();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [integrationId, integrationCategoryId]);

  const handleBind = async () => {
    if (!selectedId) {
      return;
    }
    const result = await runBind(() => serviceContractService.setIntegrationBinding(integrationId, Number(selectedId)));
    if (result !== null) {
      toast({ title: t('common.toast.successTitle'), description: t('integration.serviceContracts.bound'), variant: 'success' });
      setSelectedId('');
      void load();
    }
  };

  const handleUnbindConfirm = async () => {
    if (!unbinding) {
      return;
    }
    const result = await runUnbind(() => serviceContractService.removeIntegrationBinding(integrationId, unbinding.id));
    if (result !== null) {
      toast({ title: t('common.toast.removedTitle'), description: t('integration.serviceContracts.unbound'), variant: 'success' });
    }
    setIsConfirmOpen(false);
    setUnbinding(null);
    void load();
  };

  return (
    <div className="space-y-3">
      <div>
        <h2 className="text-lg font-semibold">{t('integration.serviceContracts.title')}</h2>
        <p className="text-sm text-muted-foreground">{t('integration.serviceContracts.description')}</p>
      </div>

      {bound.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-10 text-muted-foreground">
          <ScrollText size={40} className="mb-3 opacity-50" />
          <p className="text-sm">{t('integration.serviceContracts.empty')}</p>
        </div>
      ) : (
        <div className="space-y-2">
          {bound.map((contract) => (
            <div key={contract.id} className="flex items-center gap-3 rounded-lg border bg-card p-3">
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="truncate font-medium">{contract.name}</span>
                  {contract.hasCallback && <Badge variant="secondary" className="text-xs">callback</Badge>}
                </div>
                <code className="text-xs text-muted-foreground">{contract.identifier}</code>
              </div>
              <Button variant="ghost" size="sm" onClick={() => { setUnbinding(contract); setIsConfirmOpen(true); }}>
                <Trash2 size={16} className="text-destructive" />
              </Button>
            </div>
          ))}
        </div>
      )}

      <div className="flex items-end gap-2">
        <div className="flex-1">
          <Select value={selectedId} onValueChange={setSelectedId} disabled={available.length === 0}>
            <SelectTrigger>
              <SelectValue placeholder={available.length === 0 ? t('integration.serviceContracts.noneAvailable') : t('integration.serviceContracts.selectPlaceholder')} />
            </SelectTrigger>
            <SelectContent>
              {available.map((contract) => (
                <SelectItem key={contract.id} value={String(contract.id)}>{contract.name} ({contract.identifier})</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <Button onClick={() => void handleBind()} disabled={!selectedId || binding}>
          <Plus size={16} className="mr-2" />
          {t('integration.serviceContracts.bindAction')}
        </Button>
      </div>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleUnbindConfirm}
        title={t('integration.serviceContracts.unbindTitle')}
        description={t('integration.serviceContracts.unbindDescription').replace('{0}', unbinding?.name || '')}
        confirmText={t('integration.serviceContracts.unbindConfirm')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />
    </div>
  );
}
