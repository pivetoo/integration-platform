import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { Pipeline, CreatePipelineRequest, UpdatePipelineRequest } from '../types/pipeline';

const BASE_URL = '/Pipelines';

export const pipelineService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Pipeline>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<Pipeline[]>(`${BASE_URL}/Get${query}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getByIntegration: async (integrationId: number) => {
    const response = await httpClient.get<Pipeline[]>(`${BASE_URL}/integration/${integrationId}`);
    return response.data ?? [];
  },

  getActive: async () => {
    const response = await httpClient.get<Pipeline[]>(`${BASE_URL}/active`);
    return response.data ?? [];
  },

  getById: async (id: number) => httpClient.get<Pipeline>(`${BASE_URL}/${id}`),

  create: async (data: CreatePipelineRequest) => {
    return httpClient.post<Pipeline>(`${BASE_URL}/Create`, {
      integrationId: data.integrationId,
      identifier: data.identifier,
      name: data.name,
      description: data.description || undefined,
    });
  },

  update: async (id: number, data: UpdatePipelineRequest) => {
    return httpClient.put<Pipeline>(`${BASE_URL}/${id}`, {
      id,
      integrationId: data.integrationId,
      identifier: data.identifier,
      name: data.name,
      description: data.description || undefined,
      isActive: data.isActive,
    });
  },

  delete: (id: number) =>
    httpClient.delete<Pipeline>(`${BASE_URL}/${id}`),
};
