import type { ErrorAction, PipelineStepType } from './pipeline';

export const LogLevel = {
  Debug: 1,
  Info: 2,
  Warning: 3,
  Error: 4,
} as const;

export type LogLevel = typeof LogLevel[keyof typeof LogLevel];

export const LogLevelLabels: Record<LogLevel, string> = {
  [LogLevel.Debug]: 'Debug',
  [LogLevel.Info]: 'Info',
  [LogLevel.Warning]: 'Warning',
  [LogLevel.Error]: 'Error',
};

export interface ExecutionLogStep {
  id: number;
  order: number;
  name: string;
  type: PipelineStepType;
  errorAction: ErrorAction;
  isActive: boolean;
  ignoreOnResponse: boolean;
}

export interface ExecutionLog {
  id: number;
  executionId: number;
  pipelineStep?: ExecutionLogStep;
  level: LogLevel;
  message: string;
  context?: string;
  request?: string;
  response?: string;
  httpStatusCode?: number;
  duration?: number;
  createdAt: string;
}
