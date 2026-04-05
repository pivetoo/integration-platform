import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { IntegrationCategory, CreateIntegrationCategoryRequest, UpdateIntegrationCategoryRequest } from '../types/integrationCategory';

const BASE_URL = '/IntegrationCategories';

export const integrationCategoryService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<IntegrationCategory>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<IntegrationCategory[]>(`${BASE_URL}/Get${query}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getActive: async () => {
    const response = await httpClient.get<IntegrationCategory[]>(`${BASE_URL}/active`);
    return response.data ?? [];
  },

  getById: async (id: number) => {
    return httpClient.get<IntegrationCategory>(`${BASE_URL}/${id}`);
  },

  create: async (data: CreateIntegrationCategoryRequest) => {
    return httpClient.post<IntegrationCategory>(`${BASE_URL}/Create`, {
      name: data.name,
      description: data.description || undefined,
    });
  },

  update: async (id: number, data: UpdateIntegrationCategoryRequest) => {
    return httpClient.put<IntegrationCategory>(`${BASE_URL}/${id}`, {
      id,
      name: data.name,
      description: data.description || undefined,
      isActive: data.isActive,
    });
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
