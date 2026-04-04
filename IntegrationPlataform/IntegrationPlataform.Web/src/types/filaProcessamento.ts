import type { Conector } from './conector';
import type { Pipeline } from './pipeline';

export const StatusProcessamento = {
  Pendente: 0,
  Processando: 1,
  Concluido: 2,
  Erro: 3,
  Cancelado: 4,
} as const;

export type StatusProcessamento = typeof StatusProcessamento[keyof typeof StatusProcessamento];

export const StatusProcessamentoLabels: Record<StatusProcessamento, string> = {
  [StatusProcessamento.Pendente]: 'Pendente',
  [StatusProcessamento.Processando]: 'Processando',
  [StatusProcessamento.Concluido]: 'Concluído',
  [StatusProcessamento.Erro]: 'Erro',
  [StatusProcessamento.Cancelado]: 'Cancelado',
};

export interface FilaProcessamento {
  id: number;
  conector: Conector;
  pipeline: Pipeline;
  prioridade: number;
  status: StatusProcessamento;
  payload?: string;
  ultimoErro?: string;
  agendamento?: string;
  iniciadoEm?: string;
  finalizadoEm?: string;
  criadoEm: string;
}

export interface CreateFilaProcessamentoRequest {
  conectorId: number;
  pipelineId: number;
  prioridade: number;
  payload?: string;
  agendamento?: string;
}

export interface UpdateFilaProcessamentoRequest {
  prioridade: number;
  status: StatusProcessamento;
  payload?: string;
  ultimoErro?: string;
  agendamento?: string;
}
