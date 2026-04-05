import type { Integration } from './integracao';

export const PipelineStepType = {
  HttpRequest: 1,
  JavaScriptFunction: 2,
  ExecuteScript: 3,
} as const;

export type PipelineStepType = typeof PipelineStepType[keyof typeof PipelineStepType];

export const PipelineStepTypeLabels: Record<PipelineStepType, string> = {
  [PipelineStepType.HttpRequest]: 'Requisição HTTP',
  [PipelineStepType.JavaScriptFunction]: 'Função JavaScript',
  [PipelineStepType.ExecuteScript]: 'Executar Script SQL',
};

export const ErrorAction = {
  Stop: 1,
  Continue: 2,
} as const;

export type ErrorAction = typeof ErrorAction[keyof typeof ErrorAction];

export const ErrorActionLabels: Record<ErrorAction, string> = {
  [ErrorAction.Stop]: 'Parar',
  [ErrorAction.Continue]: 'Continuar',
};

export interface Pipeline {
  id: number;
  integrationId: number;
  integration?: Integration;
  identifier: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface PipelineStep {
  id: number;
  pipeline: Pipeline;
  order: number;
  name: string;
  type: PipelineStepType;
  apiCall?: { id: number; name: string };
  javaScriptFunction?: { id: number; name: string };
  databaseScript?: { id: number; name: string };
  errorAction: ErrorAction;
  isActive: boolean;
  ignoreOnResponse?: boolean;
}

export interface CreatePipelineRequest {
  integrationId: number;
  identifier: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface UpdatePipelineRequest {
  integrationId: number;
  identifier: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface CreatePipelineStepRequest {
  pipelineId: number;
  order: number;
  name: string;
  type: PipelineStepType;
  apiCallId?: number;
  javaScriptFunctionId?: number;
  databaseScriptId?: number;
  errorAction: ErrorAction;
  isActive: boolean;
  ignoreOnResponse?: boolean;
}

export interface UpdatePipelineStepRequest {
  pipelineId: number;
  order: number;
  name: string;
  type: PipelineStepType;
  apiCallId?: number;
  javaScriptFunctionId?: number;
  databaseScriptId?: number;
  errorAction: ErrorAction;
  isActive: boolean;
  ignoreOnResponse?: boolean;
}
