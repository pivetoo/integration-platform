import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { DebugPipelineRequest, DebugPipelineResult, Execution, ExecuteNextDebugStepRequest, ExecuteNextDebugStepResult, FinalizeDebugPipelineRequest, StartDebugPipelineRequest, StartDebugPipelineResult } from '../types/execution';

const BASE_URL = '/Executions';

export const executionService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Execution>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<Execution[]>(`${BASE_URL}/Get${query}`);
    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => httpClient.get<Execution>(`${BASE_URL}/${id}`),

  getByConnector: async (connectorId: number) => {
    return httpClient.get<Execution[]>(`${BASE_URL}/connector/${connectorId}`);
  },

  getByStatus: async (status: number) => {
    return httpClient.get<Execution[]>(`${BASE_URL}/status/${status}`);
  },

  debug: (data: DebugPipelineRequest) =>
    httpClient.post<DebugPipelineResult>(`${BASE_URL}/debug`, {
      connectorId: data.connectorId,
      pipelineId: data.pipelineId,
      inputData: data.inputData,
      initialStepId: data.initialStepId,
    }),

  startDebug: (data: StartDebugPipelineRequest) =>
    httpClient.post<StartDebugPipelineResult>(`${BASE_URL}/debug/start`, {
      connectorId: data.connectorId,
      pipelineId: data.pipelineId,
      inputData: data.inputData,
      initialStepId: data.initialStepId,
    }),

  executeNextDebugStep: (data: ExecuteNextDebugStepRequest) =>
    httpClient.post<ExecuteNextDebugStepResult>(`${BASE_URL}/debug/next-step`, {
      debugSessionId: data.debugSessionId,
    }),

  finalizeDebug: (data: FinalizeDebugPipelineRequest) =>
    httpClient.post<DebugPipelineResult>(`${BASE_URL}/debug/finish`, {
      debugSessionId: data.debugSessionId,
    }),
};
