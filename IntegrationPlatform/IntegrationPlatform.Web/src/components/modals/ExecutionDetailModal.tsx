import { useState, useEffect } from 'react';
import { Clock, AlertCircle, Info as InfoIcon, CheckCircle2, Copy, Check } from 'lucide-react';
import { Modal, ModalContent, ModalHeader, ModalTitle, Badge, useApi, useI18n } from 'archon-ui';
import type { Execution } from '../../types/execution';
import { ExecutionStatus } from '../../types/execution';
import type { ExecutionLog } from '../../types/executionLog';
import { LogLevel } from '../../types/executionLog';
import { executionService } from '../../services/executionService';
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

function formatDuracao(ms?: number | null): string {
  if (ms == null) return '-';
  if (ms < 1000) return `${ms}ms`;
  const seconds = Math.floor(ms / 1000);
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}m ${remainingSeconds}s`;
}

function tryPrettyPrint(text: string): string {
  try {
    return JSON.stringify(JSON.parse(text), null, 2);
  } catch {
    return text;
  }
}

export default function ExecutionDetailModal({ open, onOpenChange, execution }: ExecutionDetailModalProps) {
  const { t } = useI18n();
  const [currentExecution, setCurrentExecution] = useState<Execution | null>(null);
  const [logs, setLogs] = useState<ExecutionLog[]>([]);
  const [selectedLog, setSelectedLog] = useState<ExecutionLog | null>(null);
  const [copiedRequest, setCopiedRequest] = useState(false);
  const [copiedResponse, setCopiedResponse] = useState(false);

  const { execute: fetchExecution } = useApi<Execution>({ showErrorMessage: true });
  const { execute: fetchLogs, loading } = useApi<ExecutionLog[]>({ showErrorMessage: true });

  useEffect(() => {
    if (open && execution) {
      setCurrentExecution(execution);
      loadExecution(execution.id);
      loadLogs(execution.id);
    } else {
      setCurrentExecution(null);
      setLogs([]);
      setSelectedLog(null);
    }
  }, [open, execution]);

  useEffect(() => {
    if (!open || !currentExecution || currentExecution.status !== ExecutionStatus.Running) {
      return;
    }
    const intervalId = window.setInterval(() => {
      loadExecution(currentExecution.id);
      loadLogs(currentExecution.id, false);
    }, 3000);
    return () => window.clearInterval(intervalId);
  }, [open, currentExecution?.id, currentExecution?.status]);

  const loadExecution = async (executionId: number) => {
    const result = await fetchExecution(() => executionService.getById(executionId));
    if (result) {
      setCurrentExecution(result);
    }
  };

  const loadLogs = async (executionId: number, resetSelection = true) => {
    const result = await fetchLogs(() => executionLogService.getByExecution(executionId));
    if (result) {
      const sorted = [...result]
        .sort((a, b) => a.id - b.id)
        .filter((log) => !log.pipelineStep || log.duration != null);
      setLogs(sorted);
      if (resetSelection && sorted.length > 0) {
        const firstError = sorted.find((l) => l.level >= LogLevel.Error);
        const firstWithData = sorted.find((l) => l.request || l.response);
        setSelectedLog(firstError ?? firstWithData ?? sorted[0]);
      }
    }
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

  const nivelLogLabels: Record<number, string> = {
    1: t('execution.log.level.debug'),
    2: t('execution.log.level.info'),
    3: t('execution.log.level.warning'),
    4: t('execution.log.level.error'),
  };

  const getLogBadge = (log: ExecutionLog): { variant: string; icon: React.ReactNode; label: string } => {
    if (log.level === LogLevel.Error) {
      return { variant: 'destructive', icon: <AlertCircle size={14} />, label: nivelLogLabels[LogLevel.Error] };
    }
    if (log.level === LogLevel.Warning) {
      return { variant: 'warning', icon: <AlertCircle size={14} />, label: nivelLogLabels[LogLevel.Warning] };
    }
    if (log.pipelineStep && log.duration != null) {
      return { variant: 'success', icon: <CheckCircle2 size={14} />, label: t('execution.log.level.success') };
    }
    return { variant: 'secondary', icon: <InfoIcon size={14} />, label: nivelLogLabels[LogLevel.Info] };
  };

  const statusLabels: Record<number, string> = {
    1: t('execution.status.running'),
    2: t('execution.status.success'),
    3: t('execution.status.error'),
    4: t('execution.status.partial'),
  };

  if (!execution || !currentExecution) return null;

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="5xl" className="max-h-[90vh] flex flex-col">
        <ModalHeader>
          <ModalTitle className="flex items-center justify-between">
            <span>{t('execution.detail.title')} #{currentExecution.id}</span>
          </ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 mt-4 overflow-y-auto flex-1">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="font-semibold">{t('common.column.connector')}:</span> {currentExecution.connector?.name || '-'}
            </div>
            <div>
              <span className="font-semibold">{t('common.column.pipeline')}:</span> {currentExecution.pipeline?.name || '-'}
            </div>
            <div>
              <span className="font-semibold">{t('common.column.status')}:</span>{' '}
              <Badge
                variant={
                  (statusVariantMap[currentExecution.status] || 'outline') as
                    | 'warning'
                    | 'success'
                    | 'destructive'
                    | 'secondary'
                }
              >
                {statusLabels[currentExecution.status] || ExecutionStatusLabels[currentExecution.status] || '-'}
              </Badge>
            </div>
            <div>
              <span className="font-semibold">{t('common.column.duration')}:</span> {formatDuracao(currentExecution.duration)}
            </div>
          </div>

          <div className="flex gap-4 flex-1 min-h-0">
            <div className="w-80 border rounded-lg overflow-y-auto flex-shrink-0">
              <div className="sticky top-0 bg-background border-b p-3 font-semibold">
                {t('execution.detail.logsTitle')} ({logs.length})
              </div>
              {loading ? (
                <div className="p-4 text-center text-muted-foreground">{t('common.state.loading')}</div>
              ) : logs.length === 0 ? (
                <div className="p-4 text-center text-muted-foreground">{t('execution.detail.emptyLogs')}</div>
              ) : (
                <div className="divide-y">
                  {logs.map((log) => {
                    const badge = getLogBadge(log);
                    return (
                      <button
                        key={log.id}
                        onClick={() => setSelectedLog(log)}
                        className={`w-full text-left p-3 hover:bg-accent transition-colors ${selectedLog?.id === log.id ? 'bg-accent' : ''}`}
                      >
                        <div className="flex items-center justify-between mb-1 gap-2">
                          <div className="flex items-center gap-1.5 min-w-0">
                            {log.pipelineStep && (
                              <span className="text-xs font-mono text-muted-foreground shrink-0">#{log.pipelineStep.order}</span>
                            )}
                            <span className="font-medium text-sm truncate">
                              {log.pipelineStep?.name || t('execution.detail.generalLog')}
                            </span>
                          </div>
                          <Badge
                            variant={badge.variant as 'default' | 'warning' | 'destructive' | 'secondary' | 'success'}
                            className="flex items-center gap-1 shrink-0"
                          >
                            {badge.icon}
                            {badge.label}
                          </Badge>
                        </div>
                        {log.duration != null && (
                          <div className="flex items-center gap-1 text-xs text-muted-foreground">
                            <Clock size={12} />
                            {formatDuracao(log.duration)}
                          </div>
                        )}
                      </button>
                    );
                  })}
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
                        <h3 className="font-semibold flex items-center gap-2">
                          {t('execution.detail.response')}
                          {selectedLog.httpStatusCode && (
                            <Badge
                              variant={selectedLog.httpStatusCode < 400 ? 'success' : 'destructive'}
                              className="text-xs"
                            >
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
                      <pre className="text-xs bg-muted p-3 rounded overflow-x-auto">{tryPrettyPrint(selectedLog.response)}</pre>
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
                  {t('execution.detail.selectLog')}
                </div>
              )}
            </div>
          </div>
        </div>
      </ModalContent>
    </Modal>
  );
}
