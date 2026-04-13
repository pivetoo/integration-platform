import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { DatabaseScript, CreateDatabaseScriptRequest, UpdateDatabaseScriptRequest } from '../types/databaseScript';

const BASE_URL = '/DatabaseScripts';

export const databaseScriptService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<DatabaseScript>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<DatabaseScript[]>(`${BASE_URL}/Get${query}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => httpClient.get<DatabaseScript>(`${BASE_URL}/GetById/${id}`),

  create: async (data: CreateDatabaseScriptRequest) => {
    return httpClient.post<DatabaseScript>(`${BASE_URL}/Create`, {
      databaseConnectionId: data.databaseConnectionId,
      name: data.name,
      description: data.description || undefined,
      script: data.script,
    });
  },

  update: async (id: number, data: UpdateDatabaseScriptRequest) => {
    return httpClient.put<DatabaseScript>(`${BASE_URL}/Update/${id}`, {
      id,
      databaseConnectionId: data.databaseConnectionId,
      name: data.name,
      description: data.description || undefined,
      script: data.script,
    });
  },

  delete: (id: number) =>
    httpClient.delete<DatabaseScript>(`${BASE_URL}/Delete/${id}`),
};
