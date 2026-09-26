import { useState } from 'react';
import { useI18n, useToast } from 'archon-ui';

type BulkKind = 'updated' | 'deleted';

/**
 * Executa uma ação por registro em sequência e resume o resultado em um único toast.
 * Utilizado pelas ações em lote da DataTable (bulkActions) e ações por linha (rowActions).
 */
export function useBulkRun() {
  const { t } = useI18n();
  const { toast } = useToast();
  const [running, setRunning] = useState(false);

  const run = async <T,>(
    items: T[],
    action: (item: T) => Promise<unknown>,
    kind: BulkKind = 'updated',
  ): Promise<boolean> => {
    setRunning(true);
    let done = 0;
    let failed = 0;

    try {
      for (const item of items) {
        try {
          const result = await action(item);
          if (result && typeof result === 'object' && 'success' in result && (result as { success?: boolean }).success === false) {
            failed++;
          } else {
            done++;
          }
        } catch {
          failed++;
        }
      }
    } finally {
      setRunning(false);
    }

    const deletedTemplate = t('common.bulk.deleted');
    const updatedTemplate = t('common.bulk.updated');
    const partialTemplate = t('common.bulk.partial');

    if (failed === 0) {
      const description = kind === 'deleted'
        ? (deletedTemplate !== 'common.bulk.deleted' ? deletedTemplate.replace('{0}', String(done)) : `${done} registro(s) excluído(s) com sucesso.`)
        : (updatedTemplate !== 'common.bulk.updated' ? updatedTemplate.replace('{0}', String(done)) : `${done} registro(s) atualizado(s) com sucesso.`);

      toast({ title: t('common.toast.successTitle') !== 'common.toast.successTitle' ? t('common.toast.successTitle') : 'Sucesso', description, variant: 'success' });
    } else {
      const description = partialTemplate !== 'common.bulk.partial'
        ? partialTemplate.replace('{0}', String(done)).replace('{1}', String(failed))
        : `${done} processado(s), ${failed} com erro.`;

      toast({ title: 'Aviso', description, variant: done > 0 ? 'warning' : 'destructive' });
    }

    return failed === 0;
  };

  return { run, running };
}
