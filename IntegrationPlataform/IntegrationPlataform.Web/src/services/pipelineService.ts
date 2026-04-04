import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { Pipeline, CreatePipelineRequest, UpdatePipelineRequest } from '../types/pipeline';

const BASE_URL = '/Pipelines';

function mapPipeline(item: any): Pipeline {
  return {
    id: item.id,
    integracao: item.integration ? {
      id: item.integration.id,
      identificador: item.integration.identifier,
      nome: item.integration.name,
      descricao: item.integration.description,
      categoria: undefined,
      ativo: item.integration.isActive,
      criadoEm: item.integration.createdAt,
      ultimaAlteracao: item.integration.updatedAt,
    } : item.integration,
    identificador: item.identifier,
    nome: item.name,
    descricao: item.description,
    ativo: item.isActive,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

export const pipelineService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Pipeline>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);

    return {
      data: (response.data ?? []).map(mapPipeline),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getByIntegracao: async (integracaoId: number) => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/integration/${integracaoId}`);
    return response.data?.map(mapPipeline) ?? [];
  },

  getAtivos: async () => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/active`);
    return response.data?.map(mapPipeline) ?? [];
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapPipeline(response.data) : response.data,
    };
  },

  create: async (data: CreatePipelineRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      integrationId: data.integracaoId,
      identifier: data.identificador,
      name: data.nome,
      description: data.descricao || undefined,
    });

    return {
      ...response,
      data: response.data ? mapPipeline(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdatePipelineRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      integrationId: data.integracaoId,
      identifier: data.identificador,
      name: data.nome,
      description: data.descricao || undefined,
      isActive: data.ativo,
    });

    return {
      ...response,
      data: response.data ? mapPipeline(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
