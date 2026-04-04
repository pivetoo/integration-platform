import { useEffect, useMemo, useState } from 'react';
import { Check, Copy, Play, RefreshCcw, Filter, Bug } from 'lucide-react';
import { Badge, Button, Modal, ModalContent, ModalHeader, ModalTitle, useApi } from 'archon-ui';
import { conectorService } from '../../services/conectorService';
import { execucaoService } from '../../services/execucaoService';
import { execucaoLogService } from '../../services/execucaoLogService';
import { AcaoErroLabels, TipoEtapaLabels } from '../../types/pipeline';
import type { Conector } from '../../types/conector';
import type { DebugPipelineResult, ExecuteNextDebugStepResult, StartDebugPipelineResult } from '../../types/execucao';
import type { ExecucaoLog } from '../../types/execucaoLog';
import { NivelLog, NivelLogLabels } from '../../types/execucaoLog';
import type { PipelineEtapa } from '../../types/pipeline';

interface PipelineDebuggerModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  pipelineId: number;
  integracaoId: number;
  etapas: PipelineEtapa[];
  initialEtapaId?: number | null;
}

interface DebugRun {
  execucaoId: number;
  logs: ExecucaoLog[];
  output?: unknown;
}

interface BinaryStepOutput {
  isBinary: true;
  mimeType?: string;
  fileName?: string;
  size?: number;
  base64: string;
}

function prettyJson(value: unknown): string {
  if (value == null) {
    return 'null';
  }

  try {
    if (typeof value === 'string') {
      const parsed = JSON.parse(value);
      return JSON.stringify(parsed, null, 2);
    }

    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
}

function parseJsonSafe(value: unknown): unknown {
  if (value == null) {
    return null;
  }

  if (typeof value !== 'string') {
    return value;
  }

  try {
    return JSON.parse(value);
  } catch {
    return value;
  }
}

function getStepOutput(logs: ExecucaoLog[]): unknown {
  const withResponse = [...logs].reverse().find((log) => !!log.resposta);
  if (!withResponse?.resposta) {
    return null;
  }

  return parseJsonSafe(withResponse.resposta);
}

function getStepInput(logs: ExecucaoLog[]): string {
  const withRequest = [...logs].reverse().find((log) => !!log.requisicao);
  return withRequest?.requisicao ?? '';
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
  integracaoId,
  etapas,
  initialEtapaId,
}: PipelineDebuggerModalProps) {
  const [conector, setConector] = useState<Conector | null>(null);
  const [payloadText, setPayloadText] = useState<string>('{}');
  const [selectedEtapaId, setSelectedEtapaId] = useState<number | null>(initialEtapaId ?? null);
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
  const { execute: fetchLogs, loading: loadingLogs } = useApi<ExecucaoLog[]>({ showErrorMessage: true });

  useEffect(() => {
    if (!open) {
      return;
    }

    setSelectedEtapaId(initialEtapaId ?? etapas[0]?.id ?? null);
    setDebugSessionId(null);
    setDebugFlowFinished(false);
    setNextStepName(null);
    setRemainingSteps(0);

    const loadConector = async () => {
      const result = await fetchConectores(() => conectorService.getByIntegracao(integracaoId));
      if (result && result.length > 0) {
        setConector(result[0]);
      }
    };

    loadConector();
  }, [open, initialEtapaId, etapas, integracaoId]);

  const logsByEtapa = useMemo(() => {
    const map = new Map<number, ExecucaoLog[]>();

    if (!currentRun) {
      return map;
    }

    currentRun.logs.forEach((log) => {
      const etapaId = log.pipelineEtapa?.id;
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
    const map = new Map<number, ExecucaoLog[]>();

    if (!previousRun) {
      return map;
    }

    previousRun.logs.forEach((log) => {
      const etapaId = log.pipelineEtapa?.id;
      if (!etapaId) {
        return;
      }

      const list = map.get(etapaId) ?? [];
      list.push(log);
      map.set(etapaId, list);
    });

    return map;
  }, [previousRun]);

  const selectedEtapa = etapas.find((etapa) => etapa.id === selectedEtapaId) ?? null;
  const selectedLogs = selectedEtapaId ? (logsByEtapa.get(selectedEtapaId) ?? []) : [];

  const filteredLogs = selectedLogs.filter((log) => {
    if (log.nivel === NivelLog.Warning) {
      return logFilters.warning;
    }

    if (log.nivel === NivelLog.Error) {
      return logFilters.error;
    }

    return logFilters.info;
  });

  const outputAtual = getStepOutput(selectedLogs);
  const inputAtualText = getStepInput(selectedLogs);
  const outputAnterior = selectedEtapaId ? getStepOutput(previousLogsByEtapa.get(selectedEtapaId) ?? []) : null;
  const outputDiff = buildObjectDiff(outputAtual, outputAnterior);
  const binaryOutputAtual = isBinaryStepOutput(outputAtual) ? outputAtual : null;
  const outputAtualText = outputAtual == null
    ? ''
    : binaryOutputAtual
      ? prettyJson({
        isBinary: true,
        mimeType: binaryOutputAtual.mimeType,
        fileName: binaryOutputAtual.fileName,
        size: binaryOutputAtual.size,
        base64: '[omitted]',
      })
      : prettyJson(outputAtual);
  const loadingAnyAction = loadingConectores || loadingDebug || loadingLogs || loadingStartDebug || loadingNextDebugStep || loadingFinalizeDebug;

  const normalizePayload = (): string => {
    let normalizedPayload = payloadText;
    try {
      normalizedPayload = JSON.stringify(JSON.parse(payloadText));
    } catch {
    }
    return normalizedPayload;
  };

  const refreshRunLogs = async (execucaoId: number, output?: unknown) => {
    const logs = await fetchLogs(() => execucaoLogService.getByExecucao(execucaoId));
    if (!logs) {
      return;
    }

    setCurrentRun((previous) => ({
      execucaoId,
      logs,
      output: output ?? previous?.output,
    }));
  };

  const runDebug = async (etapaInicialId?: number) => {
    if (!conector) {
      return;
    }

    const normalizedPayload = normalizePayload();
    const debugResult = await executeDebug(() => execucaoService.debug({
      conectorId: conector.id,
      pipelineId,
      dadosEntrada: normalizedPayload,
      etapaInicialId,
    }));

    if (!debugResult?.id) {
      return;
    }

    setDebugSessionId(null);
    setDebugFlowFinished(false);
    setNextStepName(null);
    setRemainingSteps(0);
    setPreviousRun(currentRun);
    await refreshRunLogs(debugResult.id, debugResult.dadosSaida);

    if (etapaInicialId) {
      setSelectedEtapaId(etapaInicialId);
    }
  };

  const startStepByStepDebug = async () => {
    if (!conector || debugSessionId) {
      return;
    }

    const normalizedPayload = normalizePayload();
    const result = await startDebugSession(() => execucaoService.startDebug({
      conectorId: conector.id,
      pipelineId,
      dadosEntrada: normalizedPayload,
      etapaInicialId: selectedEtapaId ?? undefined,
    }));

    if (!result) {
      return;
    }

    setPreviousRun(currentRun);
    setCurrentRun({
      execucaoId: result.execucaoId,
      logs: [],
      output: null,
    });
    setDebugSessionId(result.debugSessionId);
    setDebugFlowFinished(result.etapasRestantes === 0);
    setRemainingSteps(result.etapasRestantes);
    setNextStepName(result.proximaEtapaNome ?? null);

    if (result.proximaEtapaId) {
      setSelectedEtapaId(result.proximaEtapaId);
    }

    await refreshRunLogs(result.execucaoId);
  };

  const executeNextStep = async () => {
    if (!debugSessionId || !currentRun) {
      return;
    }

    const result = await executeNextDebugStep(() => execucaoService.executeNextDebugStep({
      debugSessionId,
    }));

    if (!result) {
      return;
    }

    if (result.etapaExecutadaId) {
      setSelectedEtapaId(result.etapaExecutadaId);
    }

    setDebugFlowFinished(result.finalizouFluxo);
    setRemainingSteps(result.etapasRestantes);
    setNextStepName(result.proximaEtapaNome ?? null);

    await refreshRunLogs(result.execucaoId);
  };

  const finalizeStepByStepDebug = async () => {
    if (!debugSessionId || !currentRun) {
      return;
    }

    const sessionId = debugSessionId;
    const result = await finalizeDebugSession(() => execucaoService.finalizeDebug({
      debugSessionId: sessionId,
    }));

    if (!result) {
      return;
    }

    await refreshRunLogs(currentRun.execucaoId, result.dadosSaida);
    setDebugSessionId(null);
    setDebugFlowFinished(false);
    setRemainingSteps(0);
    setNextStepName(null);
  };

  const stepStatus = (etapaId: number): 'ok' | 'warn' | 'error' | 'idle' => {
    const logs = logsByEtapa.get(etapaId) ?? [];
    if (logs.length === 0) {
      return 'idle';
    }

    if (logs.some((log) => log.nivel === NivelLog.Error)) {
      return 'error';
    }

    if (logs.some((log) => log.nivel === NivelLog.Warning)) {
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
            Debugger de Pipeline
          </ModalTitle>
        </ModalHeader>

        <div className="grid gap-4 md:grid-cols-3">
          <div className="space-y-2 md:col-span-2">
            <label className="text-sm font-medium">Payload de entrada (JSON)</label>
            <textarea
              value={payloadText}
              onChange={(event) => setPayloadText(event.target.value)}
              className="h-28 w-full rounded-md border border-input bg-background px-3 py-2 text-xs font-mono text-foreground shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
              placeholder='{"clienteId": 123}'
            />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">Execução</label>
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
                Executar
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => selectedEtapaId && runDebug(selectedEtapaId)}
                disabled={!selectedEtapaId || !conector || loadingAnyAction || !!debugSessionId}
                className="h-8 px-2 text-xs"
              >
                <RefreshCcw size={13} className="mr-1" />
                Reexecutar
              </Button>
              </div>
              <div className="grid grid-cols-2 gap-2">
              <Button
                variant="outline-danger"
                size="sm"
                onClick={startStepByStepDebug}
                disabled={!selectedEtapaId || !conector || loadingAnyAction || !!debugSessionId}
                className="h-8 px-2 text-xs"
              >
                Iniciar Debug
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={executeNextStep}
                disabled={!debugSessionId || debugFlowFinished || loadingAnyAction}
                className="h-8 px-2 text-xs"
              >
                Próxima Etapa
              </Button>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={finalizeStepByStepDebug}
                disabled={!debugSessionId || loadingAnyAction}
                className="h-8 w-full px-2 text-xs"
              >
                Finalizar Debug
              </Button>
              {debugSessionId && (
                <div className="space-y-0.5 rounded border bg-muted/20 p-2 text-[11px] text-muted-foreground">
                  <p className="truncate">Sessão: {debugSessionId}</p>
                  <p>Restantes: {remainingSteps} | Próxima: {nextStepName || '-'}</p>
                </div>
              )}
              {currentRun && (
                <p className="text-[11px] text-muted-foreground">Execução atual: #{currentRun.execucaoId}</p>
              )}
            </div>
          </div>
        </div>

        <div className="mt-4 grid min-h-0 flex-1 gap-4 md:grid-cols-12">
          <div className="md:col-span-4 border rounded-lg overflow-y-auto">
            <div className="sticky top-0 border-b bg-background px-3 py-2 text-sm font-semibold">Etapas</div>
            <div className="p-2 space-y-2">
              {etapas.map((etapa) => {
                const status = stepStatus(etapa.id);
                const statusLabel = status === 'error' ? 'Erro' : status === 'warn' ? 'Alerta' : status === 'ok' ? 'OK' : 'Aguardando';
                const variant = status === 'error' ? 'destructive' : status === 'warn' ? 'warning' : status === 'ok' ? 'success' : 'outline';

                return (
                  <button
                    key={etapa.id}
                    type="button"
                    onClick={() => setSelectedEtapaId(etapa.id)}
                    className={`w-full rounded-md border p-2 text-left transition-colors ${selectedEtapaId === etapa.id ? 'border-primary bg-primary/5' : 'hover:bg-accent'}`}
                  >
                    <div className="mb-1 flex items-center justify-between gap-2">
                      <span className="text-sm font-medium truncate">{etapa.ordem}. {etapa.nome}</span>
                      <Badge variant={variant as 'outline' | 'success' | 'warning' | 'destructive'}>{statusLabel}</Badge>
                    </div>
                    <div className="flex flex-wrap gap-1 text-xs">
                      <Badge variant="outline">{TipoEtapaLabels[etapa.tipo]}</Badge>
                      <Badge variant="outline">Ao erro: {AcaoErroLabels[etapa.aoErro]}</Badge>
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
                    <span className="inline-flex items-center gap-1 text-muted-foreground"><Filter size={12} /> Filtros</span>
                    <button type="button" className={`rounded px-2 py-1 ${logFilters.info ? 'bg-accent' : 'bg-muted/40 text-muted-foreground'}`} onClick={() => setLogFilters((prev) => ({ ...prev, info: !prev.info }))}>Info</button>
                    <button type="button" className={`rounded px-2 py-1 ${logFilters.warning ? 'bg-warning/20' : 'bg-muted/40 text-muted-foreground'}`} onClick={() => setLogFilters((prev) => ({ ...prev, warning: !prev.warning }))}>Warn</button>
                    <button type="button" className={`rounded px-2 py-1 ${logFilters.error ? 'bg-destructive/20' : 'bg-muted/40 text-muted-foreground'}`} onClick={() => setLogFilters((prev) => ({ ...prev, error: !prev.error }))}>Error</button>
                  </div>
                )}
              </div>
            </div>

            <div className="flex-1 overflow-auto p-3">
              {!selectedEtapa ? (
                <div className="h-full flex items-center justify-center text-sm text-muted-foreground">Selecione uma etapa para depurar</div>
              ) : activeTab === 'input' ? (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium">Input da etapa</p>
                    <button
                      type="button"
                      onClick={() => copyText('input', inputAtualText)}
                      disabled={!inputAtualText}
                      className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs hover:bg-accent disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {copied === 'input' ? <Check size={13} /> : <Copy size={13} />} {copied === 'input' ? 'Copiado' : 'Copiar JSON'}
                    </button>
                  </div>
                  <pre className="rounded-md border bg-muted/40 p-3 text-xs overflow-auto">{inputAtualText}</pre>
                </div>
              ) : activeTab === 'output' ? (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium">Output da etapa</p>
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
                          Baixar arquivo
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={() => copyText('output', outputAtualText)}
                        disabled={!outputAtualText}
                        className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs hover:bg-accent disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        {copied === 'output' ? <Check size={13} /> : <Copy size={13} />} {copied === 'output' ? 'Copiado' : 'Copiar JSON'}
                      </button>
                    </div>
                  </div>
                  <pre className="rounded-md border bg-muted/40 p-3 text-xs overflow-auto">{outputAtualText}</pre>
                </div>
              ) : activeTab === 'diff' ? (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium">Diff com execução anterior</p>
                    <button
                      type="button"
                      onClick={() => copyText('diff', outputDiff)}
                      className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs hover:bg-accent"
                    >
                      {copied === 'diff' ? <Check size={13} /> : <Copy size={13} />} {copied === 'diff' ? 'Copiado' : 'Copiar Diff'}
                    </button>
                  </div>
                  <pre className="rounded-md border bg-muted/40 p-3 text-xs overflow-auto">{outputDiff}</pre>
                </div>
              ) : (
                <div className="space-y-2">
                  {filteredLogs.length === 0 ? (
                    <div className="rounded-md border border-dashed p-6 text-center text-sm text-muted-foreground">Nenhum log para os filtros selecionados.</div>
                  ) : (
                    filteredLogs.map((log) => (
                      <div key={log.id} className="rounded-md border p-2">
                        <div className="mb-1 flex items-center justify-between gap-2">
                          <Badge variant={log.nivel === NivelLog.Error ? 'destructive' : log.nivel === NivelLog.Warning ? 'warning' : 'secondary'}>
                            {NivelLogLabels[log.nivel]}
                          </Badge>
                          {log.duracao != null && <span className="text-xs text-muted-foreground">{log.duracao}ms</span>}
                        </div>
                        <p className="text-xs text-foreground">{log.mensagem}</p>
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
