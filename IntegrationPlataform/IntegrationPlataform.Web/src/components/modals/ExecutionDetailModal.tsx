import { useState, useEffect } from 'react';
import { Clock, AlertCircle, Info as InfoIcon, Copy, Check } from 'lucide-react';
import { Modal, ModalContent, ModalHeader, ModalTitle, Badge, useApi, useI18n } from 'archon-ui';
import type { Execution } from '../../types/execution';
import type { ExecutionLog } from '../../types/executionLog';
import { LogLevelLabels } from '../../types/executionLog';
import { executionLogService } from '../../services/executionLogService';
import { ExecutionStatusLabels } from '../../types/execution';

interface ExecutionDetailModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  execution: Execution | null;
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

export default function ExecutionDetailModal({ open, onOpenChange, execution }: ExecutionDetailModalProps) {
  const { t } = useI18n();
  const [logs, setLogs] = useState<ExecutionLog[]>([]);
  const [selectedLog, setSelectedLog] = useState<ExecutionLog | null>(null);
  const [copiedRequest, setCopiedRequest] = useState(false);
  const [copiedResponse, setCopiedResponse] = useState(false);

  const { execute: fetchLogs, loading } = useApi<ExecutionLog[]>({
    showErrorMessage: true,
  });

  useEffect(() => {
    if (open && execution) {
      loadLogs();
    } else {
      setLogs([]);
      setSelectedLog(null);
    }
  }, [open, execution]);

  const loadLogs = async () => {
    if (!execution) return;
    const result = await fetchLogs(() => executionLogService.getByExecution(execution.id));
    if (result) {
      const grouped = groupLogsByStep(result);
      setLogs(grouped);
      if (grouped.length > 0) {
        setSelectedLog(grouped[0]);
      }
    }
  };

  const groupLogsByStep = (executionLogs: ExecutionLog[]): ExecutionLog[] => {
    const stepMap = new Map<number, ExecutionLog>();
    const generalLogs: ExecutionLog[] = [];

    executionLogs.forEach((log) => {
      const stepId = log.pipelineStep?.id;

      if (!stepId) {
        generalLogs.push(log);
      } else {
        const existing = stepMap.get(stepId);
        if (!existing || log.id > existing.id) {
          stepMap.set(stepId, log);
        }
      }
    });

    return [...generalLogs, ...Array.from(stepMap.values())].sort((a, b) => a.id - b.id);
  };

  const handleCopyRequest = async () => {
    if (selectedLog?.request) {
      await navigator.clipboard.writeText(selectedLog.request);
      setCopiedRequest(true);
      setTimeout(() => setCopiedRequest(false), 2000);
    }
  };

  const handleCopyResponse = async () => {
    if (selectedLog?.response) {
      await navigator.clipboard.writeText(selectedLog.response);
      setCopiedResponse(true);
      setTimeout(() => setCopiedResponse(false), 2000);
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

  if (!execution) return null;

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="5xl" className="max-h-[90vh] flex flex-col">
        <ModalHeader>
          <ModalTitle className="flex items-center justify-between">
            <span>{t('execution.detail.title')} #{execution.id}</span>
          </ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 mt-4 overflow-y-auto flex-1">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="font-semibold">{t('common.column.connector')}:</span> {execution.connector?.name || '-'}
            </div>
            <div>
              <span className="font-semibold">{t('common.column.pipeline')}:</span> {execution.pipeline?.name || '-'}
            </div>
            <div>
              <span className="font-semibold">{t('common.column.status')}:</span>{' '}
              <Badge
                variant={
                  (statusVariantMap[execution.status] || 'outline') as
                    | 'warning'
                    | 'success'
                    | 'destructive'
                    | 'secondary'
                }
              >
                {statusLabels[execution.status] || ExecutionStatusLabels[execution.status] || '-'}
              </Badge>
            </div>
            <div>
              <span className="font-semibold">{t('common.column.duration')}:</span> {formatDuracao(execution.duration)}
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
                          {log.pipelineStep?.name || t('execution.detail.generalLog')}
                        </span>
                        <Badge
                          variant={
                            (nivelLogVariantMap[log.level] || 'outline') as
                              | 'default'
                              | 'warning'
                              | 'destructive'
                              | 'secondary'
                          }
                          className="ml-2 flex items-center gap-1"
                        >
                          {nivelLogIconMap[log.level]}
                          {nivelLogLabels[log.level] || LogLevelLabels[log.level]}
                        </Badge>
                      </div>
                      {log.duration != null && (
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                          <Clock size={12} />
                          {formatDuracao(log.duration)}
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
                    <p className="text-sm whitespace-pre-wrap bg-muted p-3 rounded">{selectedLog.message}</p>
                  </div>

                  {selectedLog.context && (
                    <div>
                      <h3 className="font-semibold mb-2">{t('execution.detail.context')}</h3>
                      <pre className="text-xs bg-muted p-3 rounded overflow-x-auto">{selectedLog.context}</pre>
                    </div>
                  )}

                  {selectedLog.request && (
                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <h3 className="font-semibold">{t('execution.detail.request')}</h3>
                        <button
                          onClick={handleCopyRequest}
                          className="flex items-center gap-1 px-2 py-1 text-xs rounded hover:bg-accent transition-colors"
                        >
                          {copiedRequest ? (
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
                      <pre className="text-xs bg-muted p-3 rounded overflow-x-auto">{selectedLog.request}</pre>
                    </div>
                  )}

                  {selectedLog.response && (
                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <h3 className="font-semibold">
                          {t('execution.detail.response')}
                          {selectedLog.httpStatusCode && (
                            <Badge variant="outline" className="ml-2">
                              HTTP {selectedLog.httpStatusCode}
                            </Badge>
                          )}
                        </h3>
                        <button
                          onClick={handleCopyResponse}
                          className="flex items-center gap-1 px-2 py-1 text-xs rounded hover:bg-accent transition-colors"
                        >
                          {copiedResponse ? (
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
                      <pre className="text-xs bg-muted p-3 rounded overflow-x-auto">{selectedLog.response}</pre>
                    </div>
                  )}

                  {selectedLog.duration != null && (
                    <div>
                      <h3 className="font-semibold mb-2">{t('common.column.duration')}</h3>
                      <p className="text-sm">{formatDuracao(selectedLog.duration)}</p>
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
