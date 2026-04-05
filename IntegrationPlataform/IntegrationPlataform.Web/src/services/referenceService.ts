import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { Reference, CreateReferenceRequest, UpdateReferenceRequest } from '../types/reference';

const BASE_URL = '/References';

export const referenceService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Reference>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<Reference[]>(`${BASE_URL}/Get${query}`);
    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => httpClient.get<Reference>(`${BASE_URL}/${id}`),

  getByConnector: async (connectorId: number) => {
    return httpClient.get<Reference[]>(`${BASE_URL}/connector/${connectorId}`);
  },

  create: (data: CreateReferenceRequest) =>
    httpClient.post<Reference>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateReferenceRequest) =>
    httpClient.put<Reference>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<Reference>(`${BASE_URL}/${id}`),
};
