import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { FuncaoJavaScript, CreateFuncaoJavaScriptRequest, UpdateFuncaoJavaScriptRequest } from '../types/funcaoJavaScript';

const BASE_URL = '/JavaScriptFunctions';

function mapFuncaoJavaScript(item: any): FuncaoJavaScript {
  return {
    id: item.id,
    nome: item.name,
    descricao: item.description ?? null,
    codigo: item.code,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

export const funcaoJavaScriptService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<FuncaoJavaScript>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);

    return {
      data: (response.data ?? []).map(mapFuncaoJavaScript),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapFuncaoJavaScript(response.data) : response.data,
    };
  },

  create: async (data: CreateFuncaoJavaScriptRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      name: data.nome,
      description: data.descricao || undefined,
      code: data.codigo,
    });

    return {
      ...response,
      data: response.data ? mapFuncaoJavaScript(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdateFuncaoJavaScriptRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      name: data.nome,
      description: data.descricao || undefined,
      code: data.codigo,
    });

    return {
      ...response,
      data: response.data ? mapFuncaoJavaScript(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
