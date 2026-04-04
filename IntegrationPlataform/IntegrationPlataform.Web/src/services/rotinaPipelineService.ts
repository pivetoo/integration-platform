import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { RotinaPipeline, CreateRotinaPipelineRequest, UpdateRotinaPipelineRequest } from '../types/rotinaPipeline';

const BASE_URL = '/rotinapipeline';

export const rotinaPipelineService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<RotinaPipeline>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<RotinaPipeline[]>(`${BASE_URL}/Get${query}`);
    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  create: (data: CreateRotinaPipelineRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateRotinaPipelineRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
