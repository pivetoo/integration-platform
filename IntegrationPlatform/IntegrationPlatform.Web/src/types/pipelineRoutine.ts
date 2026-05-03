import type { Conector } from './connector';
import type { Pipeline } from './pipeline';

export interface PipelineRoutine {
  id: number;
  connector: Conector;
  pipeline: Pipeline;
  isActive: boolean;
  intervalMinutes: number;
  defaultPayload?: string;
  lastExecution?: string;
  nextExecution?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreatePipelineRoutineRequest {
  connectorId: number;
  pipelineId: number;
  isActive: boolean;
  intervalMinutes: number;
  defaultPayload?: string;
  nextExecution?: string;
}

export interface UpdatePipelineRoutineRequest {
  isActive: boolean;
  intervalMinutes: number;
  defaultPayload?: string;
  nextExecution?: string;
}
