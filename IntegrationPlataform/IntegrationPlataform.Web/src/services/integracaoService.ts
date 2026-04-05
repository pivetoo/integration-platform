import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { Integracao, CreateIntegracaoRequest, UpdateIntegracaoRequest, IntegracaoExportModel } from '../types/integracao';

const BASE_URL = '/Integrations';

function mapIntegracao(item: any): Integracao {
  return {
    id: item.id,
    identificador: item.identifier,
    nome: item.name,
    descricao: item.description,
    categoriaId: item.integrationCategoryId,
    categoria: item.integrationCategory ? {
      id: item.integrationCategory.id,
      nome: item.integrationCategory.name,
      descricao: item.integrationCategory.description ?? null,
      ativo: item.integrationCategory.isActive,
      criadoEm: item.integrationCategory.createdAt,
      ultimaAlteracao: item.integrationCategory.updatedAt,
    } : undefined,
    ativo: item.isActive,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

export const integracaoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Integracao>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);

    return {
      data: (response.data ?? []).map(mapIntegracao),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getAtivas: async () => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/active`);
    return response.data?.map(mapIntegracao) ?? [];
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapIntegracao(response.data) : response.data,
    };
  },

  create: async (data: CreateIntegracaoRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      identifier: data.identificador,
      name: data.nome,
      description: data.descricao || undefined,
      integrationCategoryId: data.categoriaId,
    });

    return {
      ...response,
      data: response.data ? mapIntegracao(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdateIntegracaoRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      identifier: data.identificador,
      name: data.nome,
      description: data.descricao || undefined,
      integrationCategoryId: data.categoriaId,
      isActive: data.ativo,
    });

    return {
      ...response,
      data: response.data ? mapIntegracao(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),

  exportar: (id: number) =>
    httpClient.get<IntegracaoExportModel>(`${BASE_URL}/export/${id}`),

  importar: (data: IntegracaoExportModel) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/import`, data),
};
