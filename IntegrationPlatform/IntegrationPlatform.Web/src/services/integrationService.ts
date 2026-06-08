import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { Integration, CreateIntegrationRequest, UpdateIntegrationRequest, IntegrationExportModel } from '../types/integration';

const BASE_URL = '/Integrations';

export const integrationService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Integration>> => {
    const query = buildPaginationQuery(params);
    const extra: string[] = [];
    if (params?.search) { extra.push(`search=${encodeURIComponent(params.search)}`); }
    if (params?.categoryId !== undefined) { extra.push(`categoryId=${params.categoryId}`); }
    if (params?.isActive !== undefined) { extra.push(`isActive=${params.isActive}`); }
    const extraParam = extra.length > 0 ? `${query ? '&' : '?'}${extra.join('&')}` : '';
    const response = await httpClient.get<Integration[]>(`${BASE_URL}/Get${query}${extraParam}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getActive: async () => {
    const response = await httpClient.get<Integration[]>(`${BASE_URL}/active`);
    return response.data ?? [];
  },

  getById: async (id: number) => httpClient.get<Integration>(`${BASE_URL}/GetById/${id}`),

  create: async (data: CreateIntegrationRequest) => {
    return httpClient.post<Integration>(`${BASE_URL}/Create`, {
      identifier: data.identifier,
      name: data.name,
      description: data.description || undefined,
      iconUrl: data.iconUrl || undefined,
      integrationCategoryId: data.integrationCategoryId,
      isActive: data.isActive,
    });
  },

  update: async (id: number, data: UpdateIntegrationRequest) => {
    return httpClient.put<Integration>(`${BASE_URL}/Update/${id}`, {
      id,
      identifier: data.identifier,
      name: data.name,
      description: data.description || undefined,
      iconUrl: data.iconUrl || undefined,
      integrationCategoryId: data.integrationCategoryId,
      isActive: data.isActive,
    });
  },

  delete: (id: number) =>
    httpClient.delete<Integration>(`${BASE_URL}/Delete/${id}`),

  export: (id: number) =>
    httpClient.get<IntegrationExportModel>(`${BASE_URL}/export/${id}`),

  import: (data: IntegrationExportModel) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/import`, data),
};
