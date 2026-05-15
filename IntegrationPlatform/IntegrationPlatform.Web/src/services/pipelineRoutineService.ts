import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { PipelineRoutine, CreatePipelineRoutineRequest, UpdatePipelineRoutineRequest } from '../types/pipelineRoutine';

const BASE_URL = '/PipelineRoutines';

export const pipelineRoutineService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<PipelineRoutine>> => {
    const query = buildPaginationQuery(params);
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : '';
    const response = await httpClient.get<PipelineRoutine[]>(`${BASE_URL}/Get${query}${searchParam}`);
    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  create: (data: CreatePipelineRoutineRequest) =>
    httpClient.post<PipelineRoutine>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdatePipelineRoutineRequest) =>
    httpClient.put<PipelineRoutine>(`${BASE_URL}/Update/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<PipelineRoutine>(`${BASE_URL}/Delete/${id}`),
};
