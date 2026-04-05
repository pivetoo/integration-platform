import type { Conector } from './conector';
import type { Pipeline } from './pipeline';

export const ExecutionType = {
  Pipeline: 1,
  Manual: 2,
} as const;

export type ExecutionType = typeof ExecutionType[keyof typeof ExecutionType];

export const ExecutionTypeLabels: Record<ExecutionType, string> = {
  [ExecutionType.Pipeline]: 'Pipeline',
  [ExecutionType.Manual]: 'Manual',
};

export const ExecutionStatus = {
  Running: 1,
  Success: 2,
  Error: 3,
  Partial: 4,
} as const;

export type ExecutionStatus = typeof ExecutionStatus[keyof typeof ExecutionStatus];

export const ExecutionStatusLabels: Record<ExecutionStatus, string> = {
  [ExecutionStatus.Running]: 'Executando',
  [ExecutionStatus.Success]: 'Sucesso',
  [ExecutionStatus.Error]: 'Erro',
  [ExecutionStatus.Partial]: 'Parcial',
};

export interface Execution {
  id: number;
  type: ExecutionType;
  connector?: Conector;
  pipeline?: Pipeline;
  status: ExecutionStatus;
  inputData?: string;
  outputData?: string;
  errors?: string;
  startedAt: string;
  finishedAt?: string;
  duration?: number;
  createdAt: string;
  updatedAt?: string;
}

export interface DebugPipelineRequest {
  connectorId: number;
  pipelineId: number;
  inputData?: string;
  initialStepId?: number;
}

export interface DebugPipelineResult {
  id: number;
  status: ExecutionStatus;
  duration?: number;
  outputData?: unknown;
  errors?: string;
  initialStepId?: number;
}

export interface StartDebugPipelineRequest {
  connectorId: number;
  pipelineId: number;
  inputData?: string;
  initialStepId?: number;
}

export interface StartDebugPipelineResult {
  debugSessionId: string;
  executionId: number;
  status: ExecutionStatus;
  totalSteps: number;
  remainingSteps: number;
  nextStepId?: number;
  nextStepName?: string;
}

export interface ExecuteNextDebugStepRequest {
  debugSessionId: string;
}

export interface ExecuteNextDebugStepResult {
  debugSessionId: string;
  executionId: number;
  executedStep: boolean;
  executedStepId?: number;
  executedStepName?: string;
  success?: boolean;
  interruptedByError?: boolean;
  finishedFlow: boolean;
  remainingSteps: number;
  nextStepId?: number;
  nextStepName?: string;
  message?: string;
}

export interface FinalizeDebugPipelineRequest {
  debugSessionId: string;
}
