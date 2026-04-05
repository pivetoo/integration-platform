import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { Conector, CreateConectorRequest, UpdateConectorRequest } from '../types/conector';

const BASE_URL = '/Connectors';

export const conectorService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Conector>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<Conector[]>(`${BASE_URL}/Get${query}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getByIntegration: async (integrationId: number) => {
    const response = await httpClient.get<Conector[]>(`${BASE_URL}/integration/${integrationId}`);
    return response.data ?? [];
  },

  getActive: async () => {
    const response = await httpClient.get<Conector[]>(`${BASE_URL}/active`);
    return response.data ?? [];
  },

  getById: async (id: number) => {
    return httpClient.get<Conector>(`${BASE_URL}/${id}`);
  },

  create: async (data: CreateConectorRequest) => {
    return httpClient.post<Conector>(`${BASE_URL}/Create`, {
      integrationId: data.integrationId,
      name: data.name,
      systemApplicationId: data.systemApplicationId || undefined,
      isActive: data.isActive,
    });
  },

  update: async (id: number, data: UpdateConectorRequest) => {
    return httpClient.put<Conector>(`${BASE_URL}/${id}`, {
      id,
      integrationId: data.integrationId,
      name: data.name,
      systemApplicationId: data.systemApplicationId || undefined,
      isActive: data.isActive,
    });
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
