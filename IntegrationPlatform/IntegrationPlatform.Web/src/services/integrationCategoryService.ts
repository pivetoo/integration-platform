import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { IntegrationCategory, CreateIntegrationCategoryRequest, UpdateIntegrationCategoryRequest } from '../types/integrationCategory';

const BASE_URL = '/IntegrationCategories';

export const integrationCategoryService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<IntegrationCategory>> => {
    const query = buildPaginationQuery(params);
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : '';
    const response = await httpClient.get<IntegrationCategory[]>(`${BASE_URL}/Get${query}${searchParam}`);

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
    return httpClient.get<IntegrationCategory>(`${BASE_URL}/GetById/${id}`);
  },

  getByIdentifier: async (identifier: string) => {
    return httpClient.get<IntegrationCategory>(`${BASE_URL}/by-identifier/${encodeURIComponent(identifier)}`);
  },

  create: async (data: CreateIntegrationCategoryRequest) => {
    return httpClient.post<IntegrationCategory>(`${BASE_URL}/Create`, {
      identifier: data.identifier,
      name: data.name,
      description: data.description || undefined,
    });
  },

  update: async (id: number, data: UpdateIntegrationCategoryRequest) => {
    return httpClient.put<IntegrationCategory>(`${BASE_URL}/Update/${id}`, {
      id,
      identifier: data.identifier,
      name: data.name,
      description: data.description || undefined,
      isActive: data.isActive,
    });
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/Delete/${id}`),
};
