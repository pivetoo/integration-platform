import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type {
  DebugPipelineRequest,
  DebugPipelineResult,
  Execucao,
  ExecuteNextDebugStepRequest,
  ExecuteNextDebugStepResult,
  FinalizeDebugPipelineRequest,
  StartDebugPipelineRequest,
  StartDebugPipelineResult
} from '../types/execucao';

const BASE_URL = '/execucao';

export const execucaoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Execucao>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<Execucao[]>(`${BASE_URL}/Get${query}`);
    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: (id: number) => httpClient.get<Execucao>(`${BASE_URL}/${id}`),

  getByConector: (conectorId: number) =>
    httpClient.get<Execucao[]>(`${BASE_URL}/conector/${conectorId}`),

  getByStatus: (status: number) =>
    httpClient.get<Execucao[]>(`${BASE_URL}/status/${status}`),

  debug: (data: DebugPipelineRequest) =>
    httpClient.post<DebugPipelineResult>(`${BASE_URL}/debug`, data),

  startDebug: (data: StartDebugPipelineRequest) =>
    httpClient.post<StartDebugPipelineResult>(`${BASE_URL}/debug/iniciar`, data),

  executeNextDebugStep: (data: ExecuteNextDebugStepRequest) =>
    httpClient.post<ExecuteNextDebugStepResult>(`${BASE_URL}/debug/proxima-etapa`, data),

  finalizeDebug: (data: FinalizeDebugPipelineRequest) =>
    httpClient.post<DebugPipelineResult>(`${BASE_URL}/debug/finalizar`, data),
};
