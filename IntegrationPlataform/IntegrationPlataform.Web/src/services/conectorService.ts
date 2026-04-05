import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { Conector, CreateConectorRequest, UpdateConectorRequest } from '../types/conector';

const BASE_URL = '/Connectors';

function mapConector(item: any): Conector {
  return {
    id: item.id,
    sistemaId: item.systemApplicationId,
    integracaoId: item.integrationId,
    integracao: item.integration ? {
      id: item.integration.id,
      identificador: item.integration.identifier,
      nome: item.integration.name,
      descricao: item.integration.description,
      categoriaId: item.integration.integrationCategoryId,
      categoria: undefined,
      ativo: item.integration.isActive,
      criadoEm: item.integration.createdAt,
      ultimaAlteracao: item.integration.updatedAt,
    } : item.integration,
    nome: item.name,
    ativo: item.isActive,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

export const conectorService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Conector>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);

    return {
      data: (response.data ?? []).map(mapConector),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getByIntegracao: async (integracaoId: number) => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/integration/${integracaoId}`);
    return response.data?.map(mapConector) ?? [];
  },

  getAtivos: async () => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/active`);
    return response.data?.map(mapConector) ?? [];
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapConector(response.data) : response.data,
    };
  },

  create: async (data: CreateConectorRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      integrationId: data.integracaoId,
      name: data.nome,
      systemApplicationId: data.sistemaId || undefined,
    });

    return {
      ...response,
      data: response.data ? mapConector(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdateConectorRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      integrationId: data.integracaoId,
      name: data.nome,
      systemApplicationId: data.sistemaId || undefined,
      isActive: data.ativo,
    });

    return {
      ...response,
      data: response.data ? mapConector(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
