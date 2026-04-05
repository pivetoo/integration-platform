import { useEffect, useMemo, useState } from 'react';
import { Check, Copy, Play, RefreshCcw, Filter, Bug } from 'lucide-react';
import { Badge, Button, Modal, ModalContent, ModalHeader, ModalTitle, useApi, useI18n } from 'archon-ui';
import { connectorService } from '../../services/connectorService';
import { executionService } from '../../services/executionService';
import { executionLogService } from '../../services/executionLogService';
import type { Conector } from '../../types/connector';
import type { DebugPipelineResult, ExecuteNextDebugStepResult, StartDebugPipelineResult } from '../../types/execution';
import type { ExecutionLog } from '../../types/executionLog';
import { LogLevel, LogLevelLabels } from '../../types/executionLog';
import type { PipelineStep } from '../../types/pipeline';
import { normalizeJsonString, parseJsonSafe as parseJsonStringSafe, prettyJson as formatPrettyJson } from '../../utils/json';

interface PipelineDebuggerModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  pipelineId: number;
  integrationId: number;
  steps: PipelineStep[];
  initialStepId?: number | null;
}

interface DebugRun {
  executionId: number;
  logs: ExecutionLog[];
  output?: unknown;
}

interface BinaryStepOutput {
  isBinary: true;
  mimeType?: string;
  fileName?: string;
  size?: number;
  base64: string;
}

function parseJsonSafe(value: unknown): unknown {
  if (value == null) {
    return null;
  }

  if (typeof value !== 'string') {
    return value;
  }

  const parsed = parseJsonStringSafe(value);
  return parsed === null ? value : parsed;
}

function getStepOutput(logs: ExecutionLog[]): unknown {
  const withResponse = [...logs].reverse().find((log) => !!log.response);
  if (!withResponse?.response) {
    return null;
  }

  return parseJsonSafe(withResponse.response);
}

function getStepInput(logs: ExecutionLog[]): string {
  const withRequest = [...logs].reverse().find((log) => !!log.request);
  return withRequest?.request ?? '';
}

function isBinaryStepOutput(value: unknown): value is BinaryStepOutput {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const record = value as Record<string, unknown>;
  return record.isBinary === true && typeof record.base64 === 'string';
}

function downloadBase64File(base64: string, fileName: string, mimeType: string): void {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);

  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }

  const blob = new Blob([bytes], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

function buildObjectDiff(currentValue: unknown, previousValue: unknown): string {
  const current = parseJsonSafe(currentValue);
  const previous = parseJsonSafe(previousValue);

  if (typeof current !== 'object' || current == null || typeof previous !== 'object' || previous == null) {
    return 'Diff indisponível para valores não-objeto';
  }

  const currentRecord = current as Record<string, unknown>;
  const previousRecord = previous as Record<string, unknown>;
  const keys = Array.from(new Set([...Object.keys(currentRecord), ...Object.keys(previousRecord)])).sort();

  const lines: string[] = [];

  keys.forEach((key) => {
    const hasCurrent = Object.prototype.hasOwnProperty.call(currentRecord, key);
    const hasPrevious = Object.prototype.hasOwnProperty.call(previousRecord, key);

    if (!hasPrevious && hasCurrent) {
      lines.push(`+ ${key}: ${JSON.stringify(currentRecord[key])}`);
      return;
    }

    if (hasPrevious && !hasCurrent) {
      lines.push(`- ${key}: ${JSON.stringify(previousRecord[key])}`);
      return;
    }

    const currentText = JSON.stringify(currentRecord[key]);
    const previousText = JSON.stringify(previousRecord[key]);

    if (currentText !== previousText) {
      lines.push(`~ ${key}`);
      lines.push(`  anterior: ${previousText}`);
      lines.push(`  atual:    ${currentText}`);
    }
  });

  if (lines.length === 0) {
    return 'Sem diferenças em relação à execução anterior';
  }

  return lines.join('\n');
}

export default function PipelineDebuggerModal({
  open,
  onOpenChange,
  pipelineId,
  integrationId,
  steps,
  initialStepId,
}: PipelineDebuggerModalProps) {
  const { t } = useI18n();
  const [conector, setConector] = useState<Conector | null>(null);
  const [payloadText, setPayloadText] = useState<string>('{}');
  const [selectedStepId, setSelectedStepId] = useState<number | null>(initialStepId ?? null);
  const [activeTab, setActiveTab] = useState<'input' | 'output' | 'diff' | 'logs'>('output');
  const [logFilters, setLogFilters] = useState({
    info: true,
    warning: true,
    error: true,
  });
  const [copied, setCopied] = useState<'none' | 'input' | 'output' | 'diff'>('none');
  const [currentRun, setCurrentRun] = useState<DebugRun | null>(null);
  const [previousRun, setPreviousRun] = useState<DebugRun | null>(null);
  const [debugSessionId, setDebugSessionId] = useState<string | null>(null);
  const [debugFlowFinished, setDebugFlowFinished] = useState(false);
  const [nextStepName, setNextStepName] = useState<string | null>(null);
  const [remainingSteps, setRemainingSteps] = useState(0);

  const { execute: fetchConectores, loading: loadingConectores } = useApi<Conector[]>({ showErrorMessage: true });
  const { execute: executeDebug, loading: loadingDebug } = useApi<DebugPipelineResult>({ showErrorMessage: true });
  const { execute: startDebugSession, loading: loadingStartDebug } = useApi<StartDebugPipelineResult>({ showErrorMessage: true });
  const { execute: executeNextDebugStep, loading: loadingNextDebugStep } = useApi<ExecuteNextDebugStepResult>({ showErrorMessage: true });
  const { execute: finalizeDebugSession, loading: loadingFinalizeDebug } = useApi<DebugPipelineResult>({ showErrorMessage: true });
  const { execute: fetchLogs, loading: loadingLogs } = useApi<ExecutionLog[]>({ showErrorMessage: true });

  useEffect(() => {
    if (!open) {
      return;
    }

    setSelectedStepId(initialStepId ?? steps[0]?.id ?? null);
    setDebugSessionId(null);
    setDebugFlowFinished(false);
    setNextStepName(null);
    setRemainingSteps(0);

    const loadConector = async () => {
      const result = await fetchConectores(() => connectorService.getByIntegration(integrationId));
      if (result && result.length > 0) {
        setConector(result[0]);
      }
    };

    loadConector();
  }, [open, initialStepId, steps, integrationId]);

  const logsByEtapa = useMemo(() => {
    const map = new Map<number, ExecutionLog[]>();

    if (!currentRun) {
      return map;
    }

    currentRun.logs.forEach((log) => {
      const etapaId = log.pipelineStep?.id;
      if (!etapaId) {
        return;
      }

      const list = map.get(etapaId) ?? [];
      list.push(log);
      map.set(etapaId, list);
    });

    return map;
  }, [currentRun]);

  const previousLogsByEtapa = useMemo(() => {
    const map = new Map<number, ExecutionLog[]>();

    if (!previousRun) {
      return map;
    }

    previousRun.logs.forEach((log) => {
      const etapaId = log.pipelineStep?.id;
      if (!etapaId) {
        return;
      }

      const list = map.get(etapaId) ?? [];
      list.push(log);
      map.set(etapaId, list);
    });

    return map;
  }, [previousRun]);

  const selectedStep = steps.find((step) => step.id === selectedStepId) ?? null;
  const selectedLogs = selectedStepId ? (logsByEtapa.get(selectedStepId) ?? []) : [];

  const filteredLogs = selectedLogs.filter((log) => {
    if (log.level === LogLevel.Warning) {
      return logFilters.warning;
    }

    if (log.level === LogLevel.Error) {
      return logFilters.error;
    }

    return logFilters.info;
  });

  const outputAtual = getStepOutput(selectedLogs);
  const inputAtualText = getStepInput(selectedLogs);
  const outputAnterior = selectedStepId ? getStepOutput(previousLogsByEtapa.get(selectedStepId) ?? []) : null;
  const outputDiff = buildObjectDiff(outputAtual, outputAnterior);
  const binaryOutputAtual = isBinaryStepOutput(outputAtual) ? outputAtual : null;
  const outputAtualText = outputAtual == null
    ? ''
    : binaryOutputAtual
      ? formatPrettyJson({
        isBinary: true,
        mimeType: binaryOutputAtual.mimeType,
        fileName: binaryOutputAtual.fileName,
        size: binaryOutputAtual.size,
        base64: '[omitted]',
      })
      : formatPrettyJson(outputAtual);
  const loadingAnyAction = loadingConectores || loadingDebug || loadingLogs || loadingStartDebug || loadingNextDebugStep || loadingFinalizeDebug;
  const tipoEtapaLabels: Record<number, string> = {
    1: t('pipeline.step.type.httpRequest'),
    2: t('pipeline.step.type.javaScriptFunction'),
    3: t('pipeline.step.type.executeSqlScript'),
  };
  const acaoErroLabels: Record<number, string> = {
    1: t('pipeline.step.errorAction.stop'),
    2: t('pipeline.step.errorAction.continue'),
  };
  const nivelLogLabels: Record<number, string> = {
    1: t('execution.log.level.debug'),
    2: t('execution.log.level.info'),
    3: t('execution.log.level.warning'),
    4: t('execution.log.level.error'),
  };

  const normalizePayload = (): string => normalizeJsonString(payloadText);

  const refreshRunLogs = async (executionId: number, output?: unknown) => {
    const logs = await fetchLogs(() => executionLogService.getByExecution(executionId));
    if (!logs) {
      return;
    }

    setCurrentRun((previous) => ({
      executionId,
      logs,
      output: output ?? previous?.output,
    }));
  };

  const runDebug = async (initialStepIdValue?: number) => {
    if (!conector) {
      return;
    }

    const normalizedPayload = normalizePayload();
      const debugResult = await executeDebug(() => executionService.debug({
      connectorId: conector.id,
      pipelineId,
      inputData: normalizedPayload,
      initialStepId: initialStepIdValue,
    }));

    if (!debugResult?.id) {
      return;
    }

    setDebugSessionId(null);
    setDebugFlowFinished(false);
    setNextStepName(null);
    setRemainingSteps(0);
    setPreviousRun(currentRun);
    await refreshRunLogs(debugResult.id, debugResult.outputData);

    if (initialStepIdValue) {
      setSelectedStepId(initialStepIdValue);
    }
  };

  const startStepByStepDebug = async () => {
    if (!conector || debugSessionId) {
      return;
    }

    const normalizedPayload = normalizePayload();
    const result = await startDebugSession(() => executionService.startDebug({
      connectorId: conector.id,
      pipelineId,
      inputData: normalizedPayload,
      initialStepId: selectedStepId ?? undefined,
    }));

    if (!result) {
      return;
    }

    setPreviousRun(currentRun);
    setCurrentRun({
      executionId: result.executionId,
      logs: [],
      output: null,
    });
    setDebugSessionId(result.debugSessionId);
    setDebugFlowFinished(result.remainingSteps === 0);
    setRemainingSteps(result.remainingSteps);
    setNextStepName(result.nextStepName ?? null);

    if (result.nextStepId) {
      setSelectedStepId(result.nextStepId);
    }

    await refreshRunLogs(result.executionId);
  };

  const executeNextStep = async () => {
    if (!debugSessionId || !currentRun) {
      return;
    }

    const result = await executeNextDebugStep(() => executionService.executeNextDebugStep({
      debugSessionId,
    }));

    if (!result) {
      return;
    }

    if (result.executedStepId) {
      setSelectedStepId(result.executedStepId);
    }

    setDebugFlowFinished(result.finishedFlow);
    setRemainingSteps(result.remainingSteps);
    setNextStepName(result.nextStepName ?? null);

    await refreshRunLogs(result.executionId);
  };

  const finalizeStepByStepDebug = async () => {
    if (!debugSessionId || !currentRun) {
      return;
    }

    const sessionId = debugSessionId;
    const result = await finalizeDebugSession(() => executionService.finalizeDebug({
      debugSessionId: sessionId,
    }));

    if (!result) {
      return;
    }

    await refreshRunLogs(currentRun.executionId, result.outputData);
    setDebugSessionId(null);
    setDebugFlowFinished(false);
    setRemainingSteps(0);
    setNextStepName(null);
  };

  const stepStatus = (stepId: number): 'ok' | 'warn' | 'error' | 'idle' => {
    const logs = logsByEtapa.get(stepId) ?? [];
    if (logs.length === 0) {
      return 'idle';
    }

    if (logs.some((log) => log.level === LogLevel.Error)) {
      return 'error';
    }

    if (logs.some((log) => log.level === LogLevel.Warning)) {
      return 'warn';
    }

    return 'ok';
  };

  const copyText = async (kind: 'input' | 'output' | 'diff', value: string) => {
    await navigator.clipboard.writeText(value);
    setCopied(kind);
    setTimeout(() => setCopied('none'), 1500);
  };

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="5xl" className="max-h-[90vh] flex flex-col">
        <ModalHeader>
          <ModalTitle className="flex items-center gap-2">
            <Bug size={18} />
            {t('pipeline.debugger.title')}
          </ModalTitle>
        </ModalHeader>

        <div className="grid gap-4 md:grid-cols-3">
          <div className="space-y-2 md:col-span-2">
            <label className="text-sm font-medium">{t('pipeline.debugger.inputPayload')}</label>
            <textarea
              value={payloadText}
              onChange={(event) => setPayloadText(event.target.value)}
              className="h-28 w-full rounded-md border border-input bg-background px-3 py-2 text-xs font-mono text-foreground shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
              placeholder='{"clienteId": 123}'
            />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">{t('pipeline.debugger.execution')}</label>
            <div className="space-y-2 rounded-md border bg-card p-2.5">
              <div className="grid grid-cols-2 gap-2">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => runDebug()}
                disabled={!conector || loadingAnyAction || !!debugSessionId}
                className="h-8 px-2 text-xs"
              >
                <Play size={13} className="mr-1" />
                {t('pipeline.debugger.run')}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => selectedStepId && runDebug(selectedStepId)}
                disabled={!selectedStepId || !conector || loadingAnyAction || !!debugSessionId}
                className="h-8 px-2 text-xs"
              >
                <RefreshCcw size={13} className="mr-1" />
                {t('pipeline.debugger.rerun')}
              </Button>
              </div>
              <div className="grid grid-cols-2 gap-2">
              <Button
                variant="outline-danger"
                size="sm"
                onClick={startStepByStepDebug}
                disabled={!selectedStepId || !conector || loadingAnyAction || !!debugSessionId}
                className="h-8 px-2 text-xs"
              >
                {t('pipeline.debugger.startDebug')}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={executeNextStep}
                disabled={!debugSessionId || debugFlowFinished || loadingAnyAction}
                className="h-8 px-2 text-xs"
              >
                {t('pipeline.debugger.nextStep')}
              </Button>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={finalizeStepByStepDebug}
                disabled={!debugSessionId || loadingAnyAction}
                className="h-8 w-full px-2 text-xs"
              >
                {t('pipeline.debugger.finishDebug')}
              </Button>
              {debugSessionId && (
                <div className="space-y-0.5 rounded border bg-muted/20 p-2 text-[11px] text-muted-foreground">
                  <p className="truncate">{t('pipeline.debugger.session')}: {debugSessionId}</p>
                  <p>{t('pipeline.debugger.remaining')}: {remainingSteps} | {t('pipeline.debugger.next')}: {nextStepName || '-'}</p>
                </div>
              )}
              {currentRun && (
                <p className="text-[11px] text-muted-foreground">{t('pipeline.debugger.currentExecution')} #{currentRun.executionId}</p>
              )}
            </div>
          </div>
        </div>

        <div className="mt-4 grid min-h-0 flex-1 gap-4 md:grid-cols-12">
          <div className="md:col-span-4 border rounded-lg overflow-y-auto">
            <div className="sticky top-0 border-b bg-background px-3 py-2 text-sm font-semibold">{t('pipeline.detail.stepsTitle')}</div>
            <div className="p-2 space-y-2">
              {steps.map((step) => {
                const status = stepStatus(step.id);
                const statusLabel = status === 'error' ? t('pipeline.debugger.stepStatus.error') : status === 'warn' ? t('pipeline.debugger.stepStatus.warning') : status === 'ok' ? t('pipeline.debugger.stepStatus.ok') : t('pipeline.debugger.stepStatus.waiting');
                const variant = status === 'error' ? 'destructive' : status === 'warn' ? 'warning' : status === 'ok' ? 'success' : 'outline';

                return (
                  <button
                    key={step.id}
                    type="button"
                    onClick={() => setSelectedStepId(step.id)}
                    className={`w-full rounded-md border p-2 text-left transition-colors ${selectedStepId === step.id ? 'border-primary bg-primary/5' : 'hover:bg-accent'}`}
                  >
                    <div className="mb-1 flex items-center justify-between gap-2">
                      <span className="text-sm font-medium truncate">{step.order}. {step.name}</span>
                      <Badge variant={variant as 'outline' | 'success' | 'warning' | 'destructive'}>{statusLabel}</Badge>
                    </div>
                    <div className="flex flex-wrap gap-1 text-xs">
                      <Badge variant="outline">{tipoEtapaLabels[step.type] || '-'}</Badge>
                      <Badge variant="outline">{t('pipeline.detail.onErrorLabel')}: {acaoErroLabels[step.errorAction] || '-'}</Badge>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="md:col-span-8 border rounded-lg overflow-hidden flex flex-col min-h-0">
            <div className="border-b bg-background px-3 py-2">
              <div className="flex flex-wrap items-center gap-2">
                <button type="button" className={`rounded-md px-2 py-1 text-sm ${activeTab === 'input' ? 'bg-accent font-medium' : 'text-muted-foreground'}`} onClick={() => setActiveTab('input')}>Input</button>
                <button type="button" className={`rounded-md px-2 py-1 text-sm ${activeTab === 'output' ? 'bg-accent font-medium' : 'text-muted-foreground'}`} onClick={() => setActiveTab('output')}>Output</button>
                <button type="button" className={`rounded-md px-2 py-1 text-sm ${activeTab === 'diff' ? 'bg-accent font-medium' : 'text-muted-foreground'}`} onClick={() => setActiveTab('diff')}>Diff</button>
                <button type="button" className={`rounded-md px-2 py-1 text-sm ${activeTab === 'logs' ? 'bg-accent font-medium' : 'text-muted-foreground'}`} onClick={() => setActiveTab('logs')}>Logs</button>

                {activeTab === 'logs' && (
                  <div className="ml-auto flex items-center gap-1 text-xs">
                    <span className="inline-flex items-center gap-1 text-muted-foreground"><Filter size={12} /> {t('pipeline.debugger.filters')}</span>
                    <button type="button" className={`rounded px-2 py-1 ${logFilters.info ? 'bg-accent' : 'bg-muted/40 text-muted-foreground'}`} onClick={() => setLogFilters((prev) => ({ ...prev, info: !prev.info }))}>{t('execution.log.level.info')}</button>
                    <button type="button" className={`rounded px-2 py-1 ${logFilters.warning ? 'bg-warning/20' : 'bg-muted/40 text-muted-foreground'}`} onClick={() => setLogFilters((prev) => ({ ...prev, warning: !prev.warning }))}>{t('execution.log.level.warning')}</button>
                    <button type="button" className={`rounded px-2 py-1 ${logFilters.error ? 'bg-destructive/20' : 'bg-muted/40 text-muted-foreground'}`} onClick={() => setLogFilters((prev) => ({ ...prev, error: !prev.error }))}>{t('execution.log.level.error')}</button>
                  </div>
                )}
              </div>
            </div>

            <div className="flex-1 overflow-auto p-3">
              {!selectedStep ? (
                <div className="h-full flex items-center justify-center text-sm text-muted-foreground">{t('pipeline.debugger.selectStep')}</div>
              ) : activeTab === 'input' ? (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium">{t('pipeline.debugger.stepInput')}</p>
                    <button
                      type="button"
                      onClick={() => copyText('input', inputAtualText)}
                      disabled={!inputAtualText}
                      className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs hover:bg-accent disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {copied === 'input' ? <Check size={13} /> : <Copy size={13} />} {copied === 'input' ? t('common.action.copied') : t('pipeline.debugger.copyJson')}
                    </button>
                  </div>
                  <pre className="rounded-md border bg-muted/40 p-3 text-xs overflow-auto">{inputAtualText}</pre>
                </div>
              ) : activeTab === 'output' ? (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium">{t('pipeline.debugger.stepOutput')}</p>
                    <div className="flex items-center gap-2">
                      {binaryOutputAtual && (
                        <button
                          type="button"
                          onClick={() => downloadBase64File(
                            binaryOutputAtual.base64,
                            binaryOutputAtual.fileName || 'arquivo.bin',
                            binaryOutputAtual.mimeType || 'application/octet-stream'
                          )}
                          className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs hover:bg-accent"
                        >
                          {t('pipeline.debugger.downloadFile')}
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={() => copyText('output', outputAtualText)}
                        disabled={!outputAtualText}
                        className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs hover:bg-accent disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        {copied === 'output' ? <Check size={13} /> : <Copy size={13} />} {copied === 'output' ? t('common.action.copied') : t('pipeline.debugger.copyJson')}
                      </button>
                    </div>
                  </div>
                  <pre className="rounded-md border bg-muted/40 p-3 text-xs overflow-auto">{outputAtualText}</pre>
                </div>
              ) : activeTab === 'diff' ? (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium">{t('pipeline.debugger.diffWithPrevious')}</p>
                    <button
                      type="button"
                      onClick={() => copyText('diff', outputDiff)}
                      className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs hover:bg-accent"
                    >
                      {copied === 'diff' ? <Check size={13} /> : <Copy size={13} />} {copied === 'diff' ? t('common.action.copied') : t('pipeline.debugger.copyDiff')}
                    </button>
                  </div>
                  <pre className="rounded-md border bg-muted/40 p-3 text-xs overflow-auto">{outputDiff}</pre>
                </div>
              ) : (
                <div className="space-y-2">
                  {filteredLogs.length === 0 ? (
                    <div className="rounded-md border border-dashed p-6 text-center text-sm text-muted-foreground">{t('pipeline.debugger.emptyLogs')}</div>
                  ) : (
                    filteredLogs.map((log) => (
                      <div key={log.id} className="rounded-md border p-2">
                        <div className="mb-1 flex items-center justify-between gap-2">
                          <Badge variant={log.level === LogLevel.Error ? 'destructive' : log.level === LogLevel.Warning ? 'warning' : 'secondary'}>
                            {nivelLogLabels[log.level] || LogLevelLabels[log.level]}
                          </Badge>
                          {log.duration != null && <span className="text-xs text-muted-foreground">{log.duration}ms</span>}
                        </div>
                        <p className="text-xs text-foreground">{log.message}</p>
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </ModalContent>
    </Modal>
  );
}
