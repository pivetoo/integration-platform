import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { ApiCall, CreateApiCallRequest, UpdateApiCallRequest } from '../types/apiCall';

const BASE_URL = '/ApiCalls';

export const apiCallService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<ApiCall>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<ApiCall[]>(`${BASE_URL}/Get${query}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => {
    return httpClient.get<ApiCall>(`${BASE_URL}/${id}`);
  },

  create: async (data: CreateApiCallRequest) => {
    return httpClient.post<ApiCall>(`${BASE_URL}/Create`, {
      name: data.name,
      description: data.description || undefined,
      method: data.method,
      url: data.url,
      headersTemplate: data.headersTemplate || undefined,
      bodyTemplate: data.bodyTemplate || undefined,
    });
  },

  update: async (id: number, data: UpdateApiCallRequest) => {
    return httpClient.put<ApiCall>(`${BASE_URL}/${id}`, {
      id,
      name: data.name,
      description: data.description || undefined,
      method: data.method,
      url: data.url,
      headersTemplate: data.headersTemplate || undefined,
      bodyTemplate: data.bodyTemplate || undefined,
    });
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
