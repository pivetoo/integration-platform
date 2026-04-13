import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { DatabaseConnection, CreateDatabaseConnectionRequest, UpdateDatabaseConnectionRequest } from '../types/databaseConnection';

const BASE_URL = '/DatabaseConnections';

export const databaseConnectionService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<DatabaseConnection>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<DatabaseConnection[]>(`${BASE_URL}/Get${query}`);

    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => {
    return httpClient.get<DatabaseConnection>(`${BASE_URL}/GetById/${id}`);
  },

  create: async (data: CreateDatabaseConnectionRequest) => {
    return httpClient.post<DatabaseConnection>(`${BASE_URL}/Create`, {
      name: data.name,
      type: data.type,
      host: data.host,
      port: data.port,
      database: data.database,
      username: data.username,
      password: data.password,
    });
  },

  update: async (id: number, data: UpdateDatabaseConnectionRequest) => {
    return httpClient.put<DatabaseConnection>(`${BASE_URL}/Update/${id}`, {
      id,
      name: data.name,
      type: data.type,
      host: data.host,
      port: data.port,
      database: data.database,
      username: data.username,
      password: data.password,
    });
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/Delete/${id}`),

  test: (data: CreateDatabaseConnectionRequest) =>
    httpClient.post<{ message: string }>(`${BASE_URL}/test`, {
      name: data.name,
      type: data.type,
      host: data.host,
      port: data.port,
      database: data.database,
      username: data.username,
      password: data.password,
    }),
};
