import { useState, useEffect } from 'react';
import { Clock, AlertCircle, Info as InfoIcon, Copy, Check } from 'lucide-react';
import { Modal, ModalContent, ModalHeader, ModalTitle, Badge, useApi, useI18n } from 'archon-ui';
import type { Execucao } from '../../types/execucao';
import type { ExecucaoLog } from '../../types/execucaoLog';
import { NivelLogLabels } from '../../types/execucaoLog';
import { execucaoLogService } from '../../services/execucaoLogService';
import { StatusExecucaoLabels } from '../../types/execucao';

interface ExecucaoDetalheModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  execucao: Execucao | null;
}

const statusVariantMap: Record<number, string> = {
  1: 'warning',
  2: 'success',
  3: 'destructive',
  4: 'secondary',
};

const nivelLogVariantMap: Record<number, string> = {
  1: 'secondary',
  2: 'default',
  3: 'warning',
  4: 'destructive',
};

const nivelLogIconMap: Record<number, React.ReactNode> = {
  1: <InfoIcon size={16} />,
  2: <InfoIcon size={16} />,
  3: <AlertCircle size={16} />,
  4: <AlertCircle size={16} />,
};

function formatDuracao(ms?: number): string {
  if (ms == null) return '-';
  if (ms < 1000) return `${ms}ms`;
  const seconds = Math.floor(ms / 1000);
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}m ${remainingSeconds}s`;
}

export default function ExecucaoDetalheModal({ open, onOpenChange, execucao }: ExecucaoDetalheModalProps) {
  const { t } = useI18n();
  const [logs, setLogs] = useState<ExecucaoLog[]>([]);
  const [selectedLog, setSelectedLog] = useState<ExecucaoLog | null>(null);
  const [copiedRequisicao, setCopiedRequisicao] = useState(false);
  const [copiedResposta, setCopiedResposta] = useState(false);

  const { execute: fetchLogs, loading } = useApi<ExecucaoLog[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open && execucao) {
      loadLogs();
    } else {
      setLogs([]);
      setSelectedLog(null);
    }
  }, [open, execucao]);

  const loadLogs = async () => {
    if (!execucao) return;
    const result = await fetchLogs(() => execucaoLogService.getByExecucao(execucao.id));
    if (result) {
      const grouped = groupLogsByEtapa(result);
      setLogs(grouped);
      if (grouped.length > 0) {
        setSelectedLog(grouped[0]);
      }
    }
  };

  const groupLogsByEtapa = (logs: ExecucaoLog[]): ExecucaoLog[] => {
    const etapaMap = new Map<number, ExecucaoLog>();
    const logsGerais: ExecucaoLog[] = [];

    logs.forEach((log) => {
      const etapaId = log.pipelineEtapa?.id;

      if (!etapaId) {
        logsGerais.push(log);
      } else {
        const existing = etapaMap.get(etapaId);
        if (!existing || log.id > existing.id) {
          etapaMap.set(etapaId, log);
        }
      }
    });

    return [...logsGerais, ...Array.from(etapaMap.values())].sort((a, b) => a.id - b.id);
  };

  const handleCopyRequisicao = async () => {
    if (selectedLog?.requisicao) {
      await navigator.clipboard.writeText(selectedLog.requisicao);
      setCopiedRequisicao(true);
      setTimeout(() => setCopiedRequisicao(false), 2000);
    }
  };

  const handleCopyResposta = async () => {
    if (selectedLog?.resposta) {
      await navigator.clipboard.writeText(selectedLog.resposta);
      setCopiedResposta(true);
      setTimeout(() => setCopiedResposta(false), 2000);
    }
  };

  const statusLabels: Record<number, string> = {
    1: t('execution.status.running'),
    2: t('execution.status.success'),
    3: t('execution.status.error'),
    4: t('execution.status.partial'),
  };

  const nivelLogLabels: Record<number, string> = {
    1: t('execution.log.level.debug'),
    2: t('execution.log.level.info'),
    3: t('execution.log.level.warning'),
    4: t('execution.log.level.error'),
  };

  if (!execucao) return null;

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="5xl" className="max-h-[90vh] flex flex-col">
        <ModalHeader>
          <ModalTitle className="flex items-center justify-between">
            <span>{t('execution.detail.title')} #{execucao.id}</span>
          </ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 mt-4 overflow-y-auto flex-1">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="font-semibold">{t('common.column.connector')}:</span> {execucao.conector?.nome || '-'}
            </div>
            <div>
              <span className="font-semibold">{t('common.column.pipeline')}:</span> {execucao.pipeline?.nome || '-'}
            </div>
            <div>
              <span className="font-semibold">{t('common.column.status')}:</span>{' '}
              <Badge
                variant={
                  (statusVariantMap[execucao.status] || 'outline') as
                    | 'warning'
                    | 'success'
                    | 'destructive'
                    | 'secondary'
                }
              >
                {statusLabels[execucao.status] || StatusExecucaoLabels[execucao.status] || '-'}
              </Badge>
            </div>
            <div>
              <span className="font-semibold">{t('common.column.duration')}:</span> {formatDuracao(execucao.duracao)}
            </div>
          </div>

          <div className="flex gap-4 flex-1 min-h-0">
            <div className="w-80 border rounded-lg overflow-y-auto flex-shrink-0">
              <div className="sticky top-0 bg-background border-b p-3 font-semibold">
                {t('pipeline.detail.stepsTitle')} ({logs.length})
              </div>
              {loading ? (
                <div className="p-4 text-center text-muted-foreground">{t('common.state.loading')}</div>
              ) : logs.length === 0 ? (
                <div className="p-4 text-center text-muted-foreground">{t('execution.detail.emptyLogs')}</div>
              ) : (
                <div className="divide-y">
                  {logs.map((log) => (
                    <button
                      key={log.id}
                      onClick={() => setSelectedLog(log)}
                      className={`w-full text-left p-3 hover:bg-accent transition-colors ${
                        selectedLog?.id === log.id ? 'bg-accent' : ''
                      }`}
                    >
                      <div className="flex items-center justify-between mb-1">
                        <span className="font-medium text-sm truncate">
                          {log.pipelineEtapa?.nome || t('execution.detail.generalLog')}
                        </span>
                        <Badge
                          variant={
                            (nivelLogVariantMap[log.nivel] || 'outline') as
                              | 'default'
                              | 'warning'
                              | 'destructive'
                              | 'secondary'
                          }
                          className="ml-2 flex items-center gap-1"
                        >
                          {nivelLogIconMap[log.nivel]}
                          {nivelLogLabels[log.nivel] || NivelLogLabels[log.nivel]}
                        </Badge>
                      </div>
                      {log.duracao != null && (
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                          <Clock size={12} />
                          {formatDuracao(log.duracao)}
                        </div>
                      )}
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="flex-1 border rounded-lg overflow-y-auto">
              {selectedLog ? (
                <div className="p-4 space-y-4">
                  <div>
                    <h3 className="font-semibold mb-2">{t('execution.detail.message')}</h3>
                    <p className="text-sm whitespace-pre-wrap bg-muted p-3 rounded">{selectedLog.mensagem}</p>
                  </div>

                  {selectedLog.contexto && (
                    <div>
                      <h3 className="font-semibold mb-2">{t('execution.detail.context')}</h3>
                      <pre className="text-xs bg-muted p-3 rounded overflow-x-auto">{selectedLog.contexto}</pre>
                    </div>
                  )}

                  {selectedLog.requisicao && (
                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <h3 className="font-semibold">{t('execution.detail.request')}</h3>
                        <button
                          onClick={handleCopyRequisicao}
                          className="flex items-center gap-1 px-2 py-1 text-xs rounded hover:bg-accent transition-colors"
                        >
                          {copiedRequisicao ? (
                            <>
                              <Check size={14} className="text-green-600" />
                              <span>{t('common.action.copied')}</span>
                            </>
                          ) : (
                            <>
                              <Copy size={14} />
                              <span>{t('common.action.copy')}</span>
                            </>
                          )}
                        </button>
                      </div>
                      <pre className="text-xs bg-muted p-3 rounded overflow-x-auto">{selectedLog.requisicao}</pre>
                    </div>
                  )}

                  {selectedLog.resposta && (
                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <h3 className="font-semibold">
                          {t('execution.detail.response')}
                          {selectedLog.statusHttpCode && (
                            <Badge variant="outline" className="ml-2">
                              HTTP {selectedLog.statusHttpCode}
                            </Badge>
                          )}
                        </h3>
                        <button
                          onClick={handleCopyResposta}
                          className="flex items-center gap-1 px-2 py-1 text-xs rounded hover:bg-accent transition-colors"
                        >
                          {copiedResposta ? (
                            <>
                              <Check size={14} className="text-green-600" />
                              <span>{t('common.action.copied')}</span>
                            </>
                          ) : (
                            <>
                              <Copy size={14} />
                              <span>{t('common.action.copy')}</span>
                            </>
                          )}
                        </button>
                      </div>
                      <pre className="text-xs bg-muted p-3 rounded overflow-x-auto">{selectedLog.resposta}</pre>
                    </div>
                  )}

                  {selectedLog.duracao != null && (
                    <div>
                      <h3 className="font-semibold mb-2">{t('common.column.duration')}</h3>
                      <p className="text-sm">{formatDuracao(selectedLog.duracao)}</p>
                    </div>
                  )}
                </div>
              ) : (
                <div className="flex items-center justify-center h-full text-muted-foreground">
                  {t('execution.detail.selectStep')}
                </div>
              )}
            </div>
          </div>
        </div>
      </ModalContent>
    </Modal>
  );
}
