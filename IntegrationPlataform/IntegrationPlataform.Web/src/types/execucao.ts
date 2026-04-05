import type { Conector } from './conector';
import type { Pipeline } from './pipeline';

export const TipoExecucao = {
  Pipeline: 1,
  Manual: 2,
} as const;

export type TipoExecucao = typeof TipoExecucao[keyof typeof TipoExecucao];

export const TipoExecucaoLabels: Record<TipoExecucao, string> = {
  [TipoExecucao.Pipeline]: 'Pipeline',
  [TipoExecucao.Manual]: 'Manual',
};

export const StatusExecucao = {
  Executando: 1,
  Sucesso: 2,
  Erro: 3,
  Parcial: 4,
} as const;

export type StatusExecucao = typeof StatusExecucao[keyof typeof StatusExecucao];

export const StatusExecucaoLabels: Record<StatusExecucao, string> = {
  [StatusExecucao.Executando]: 'Executando',
  [StatusExecucao.Sucesso]: 'Sucesso',
  [StatusExecucao.Erro]: 'Erro',
  [StatusExecucao.Parcial]: 'Parcial',
};

export interface Execucao {
  id: number;
  tipo: TipoExecucao;
  conector?: Conector;
  pipeline?: Pipeline;
  status: StatusExecucao;
  dadosEntrada?: string;
  dadosSaida?: string;
  erros?: string;
  iniciadoEm: string;
  finalizadoEm?: string;
  duracao?: number;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface DebugPipelineRequest {
  conectorId: number;
  pipelineId: number;
  dadosEntrada?: string;
  etapaInicialId?: number;
}

export interface DebugPipelineResult {
  id: number;
  status: StatusExecucao;
  duracao?: number;
  dadosSaida?: unknown;
  erros?: string;
  etapaInicialId?: number;
}

export interface StartDebugPipelineRequest {
  conectorId: number;
  pipelineId: number;
  dadosEntrada?: string;
  etapaInicialId?: number;
}

export interface StartDebugPipelineResult {
  debugSessionId: string;
  execucaoId: number;
  status: StatusExecucao;
  totalEtapas: number;
  etapasRestantes: number;
  proximaEtapaId?: number;
  proximaEtapaNome?: string;
}

export interface ExecuteNextDebugStepRequest {
  debugSessionId: string;
}

export interface ExecuteNextDebugStepResult {
  debugSessionId: string;
  execucaoId: number;
  executouEtapa: boolean;
  etapaExecutadaId?: number;
  etapaExecutadaNome?: string;
  sucesso?: boolean;
  interrompidoPorErro?: boolean;
  finalizouFluxo: boolean;
  etapasRestantes: number;
  proximaEtapaId?: number;
  proximaEtapaNome?: string;
  mensagem?: string;
}

export interface FinalizeDebugPipelineRequest {
  debugSessionId: string;
}
