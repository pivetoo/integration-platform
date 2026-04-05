import { httpClient } from 'archon-ui';
import type { PipelineStep, CreatePipelineStepRequest, UpdatePipelineStepRequest } from '../types/pipeline';

const BASE_URL = '/PipelineSteps';

export const pipelineEtapaService = {
  getByPipeline: async (pipelineId: number) => {
    const response = await httpClient.get<PipelineStep[]>(`${BASE_URL}/pipeline/${pipelineId}`);
    return response.data ?? [];
  },

  getById: async (id: number) => httpClient.get<PipelineStep>(`${BASE_URL}/${id}`),

  create: async (data: CreatePipelineStepRequest) => {
    return httpClient.post<PipelineStep>(`${BASE_URL}/Create`, {
      pipelineId: data.pipelineId,
      order: data.order,
      name: data.name,
      type: data.type,
      apiCallId: data.apiCallId,
      javaScriptFunctionId: data.javaScriptFunctionId,
      databaseScriptId: data.databaseScriptId,
      errorAction: data.errorAction,
      ignoreOnResponse: data.ignoreOnResponse ?? false,
    });
  },

  update: async (id: number, data: UpdatePipelineStepRequest) => {
    return httpClient.put<PipelineStep>(`${BASE_URL}/${id}`, {
      id,
      order: data.order,
      name: data.name,
      type: data.type,
      apiCallId: data.apiCallId,
      javaScriptFunctionId: data.javaScriptFunctionId,
      databaseScriptId: data.databaseScriptId,
      errorAction: data.errorAction,
      isActive: data.isActive,
      ignoreOnResponse: data.ignoreOnResponse ?? false,
    });
  },

  delete: (id: number) =>
    httpClient.delete<PipelineStep>(`${BASE_URL}/${id}`),

  reorder: (steps: Array<{ id: number; order: number }>) =>
    httpClient.post<{ message: string }>(`${BASE_URL}/Reorder`, { steps: steps.map((step) => ({ id: step.id, order: step.order })) }),
};
