import type { Conector } from './connector';
import type { Pipeline } from './pipeline';

export const ProcessingStatus = {
  Pending: 1,
  Processing: 2,
  Completed: 3,
  Error: 4,
  Cancelled: 5,
} as const;

export type ProcessingStatus = typeof ProcessingStatus[keyof typeof ProcessingStatus];

export const ProcessingStatusLabels: Record<ProcessingStatus, string> = {
  [ProcessingStatus.Pending]: 'Pendente',
  [ProcessingStatus.Processing]: 'Processando',
  [ProcessingStatus.Completed]: 'Concluído',
  [ProcessingStatus.Error]: 'Erro',
  [ProcessingStatus.Cancelled]: 'Cancelado',
};

export interface ProcessingQueueItem {
  id: number;
  connector?: Conector;
  pipeline?: Pipeline;
  priority: number;
  status: ProcessingStatus;
  payload?: string;
  lastError?: string;
  attempts: number;
  idempotencyKey?: string;
  scheduledAt?: string;
  startedAt?: string;
  finishedAt?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateProcessingQueueItemRequest {
  connectorId: number;
  pipelineId: number;
  priority: number;
  payload?: string;
  scheduledAt?: string;
}
