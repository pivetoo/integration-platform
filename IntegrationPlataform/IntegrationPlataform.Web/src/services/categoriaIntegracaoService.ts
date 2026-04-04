import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { CategoriaIntegracao, CreateCategoriaIntegracaoRequest, UpdateCategoriaIntegracaoRequest } from '../types/categoriaIntegracao';

const BASE_URL = '/IntegrationCategories';

function mapCategoriaIntegracao(item: any): CategoriaIntegracao {
  return {
    id: item.id,
    nome: item.name,
    descricao: item.description ?? null,
    ativo: item.isActive,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

export const categoriaIntegracaoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<CategoriaIntegracao>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);

    return {
      data: (response.data ?? []).map(mapCategoriaIntegracao),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getAtivas: async () => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/active`);
    return response.data?.map(mapCategoriaIntegracao) ?? [];
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapCategoriaIntegracao(response.data) : response.data,
    };
  },

  create: async (data: CreateCategoriaIntegracaoRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      name: data.nome,
      description: data.descricao || undefined,
    });

    return {
      ...response,
      data: response.data ? mapCategoriaIntegracao(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdateCategoriaIntegracaoRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      name: data.nome,
      description: data.descricao || undefined,
      isActive: data.ativo,
    });

    return {
      ...response,
      data: response.data ? mapCategoriaIntegracao(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
