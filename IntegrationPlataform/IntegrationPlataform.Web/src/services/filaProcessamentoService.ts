import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { FilaProcessamento, CreateFilaProcessamentoRequest, UpdateFilaProcessamentoRequest } from '../types/filaProcessamento';

const BASE_URL = '/filaprocessamento';

export const filaProcessamentoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<FilaProcessamento>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<FilaProcessamento[]>(`${BASE_URL}/Get${query}`);
    return {
      data: response.data ?? [],
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: (id: number) => httpClient.get<FilaProcessamento>(`${BASE_URL}/${id}`),

  getPendentes: () => httpClient.get<FilaProcessamento[]>(`${BASE_URL}/pendentes`),

  create: (data: CreateFilaProcessamentoRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateFilaProcessamentoRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
