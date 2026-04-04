import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { ChamadaApi, CreateChamadaApiRequest, UpdateChamadaApiRequest } from '../types/chamadaApi';

const BASE_URL = '/ApiCalls';

function mapChamadaApi(item: any): ChamadaApi {
  return {
    id: item.id,
    nome: item.name,
    descricao: item.description,
    metodo: item.method,
    url: item.url,
    headersTemplate: item.headersTemplate,
    bodyTemplate: item.bodyTemplate,
  };
}

export const chamadaApiService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<ChamadaApi>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);

    return {
      data: (response.data ?? []).map(mapChamadaApi),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapChamadaApi(response.data) : response.data,
    };
  },

  create: async (data: CreateChamadaApiRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      name: data.nome,
      description: data.descricao || undefined,
      method: data.metodo,
      url: data.url,
      headersTemplate: data.headersTemplate || undefined,
      bodyTemplate: data.bodyTemplate || undefined,
    });

    return {
      ...response,
      data: response.data ? mapChamadaApi(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdateChamadaApiRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      name: data.nome,
      description: data.descricao || undefined,
      method: data.metodo,
      url: data.url,
      headersTemplate: data.headersTemplate || undefined,
      bodyTemplate: data.bodyTemplate || undefined,
    });

    return {
      ...response,
      data: response.data ? mapChamadaApi(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
