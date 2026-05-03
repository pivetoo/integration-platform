import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { JavaScriptFunction, CreateJavaScriptFunctionRequest, UpdateJavaScriptFunctionRequest } from '../types/javaScriptFunction';

const BASE_URL = '/JavaScriptFunctions';

export const javaScriptFunctionService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<JavaScriptFunction>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<JavaScriptFunction[]>(`${BASE_URL}/Get${query}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => {
    return httpClient.get<JavaScriptFunction>(`${BASE_URL}/GetById/${id}`);
  },

  create: async (data: CreateJavaScriptFunctionRequest) => {
    return httpClient.post<JavaScriptFunction>(`${BASE_URL}/Create`, {
      name: data.name,
      description: data.description || undefined,
      code: data.code,
    });
  },

  update: async (id: number, data: UpdateJavaScriptFunctionRequest) => {
    return httpClient.put<JavaScriptFunction>(`${BASE_URL}/Update/${id}`, {
      id,
      name: data.name,
      description: data.description || undefined,
      code: data.code,
    });
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/Delete/${id}`),
};
