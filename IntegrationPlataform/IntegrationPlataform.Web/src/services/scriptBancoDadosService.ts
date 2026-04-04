import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { ScriptBancoDados, CreateScriptBancoDadosRequest, UpdateScriptBancoDadosRequest } from '../types/scriptBancoDados';

const BASE_URL = '/DatabaseScripts';

function mapScriptBancoDados(item: any): ScriptBancoDados {
  return {
    id: item.id,
    conexaoBancoDados: {
      id: item.databaseConnection?.id,
      nome: item.databaseConnection?.name,
      tipo: item.databaseConnection?.type,
      host: item.databaseConnection?.host,
      port: item.databaseConnection?.port,
      database: item.databaseConnection?.database,
      username: item.databaseConnection?.username,
      password: item.databaseConnection?.password,
      criadoEm: item.databaseConnection?.createdAt,
      ultimaAlteracao: item.databaseConnection?.updatedAt,
    },
    nome: item.name,
    descricao: item.description ?? null,
    script: item.script,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

export const scriptBancoDadosService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<ScriptBancoDados>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);

    return {
      data: (response.data ?? []).map(mapScriptBancoDados),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapScriptBancoDados(response.data) : response.data,
    };
  },

  create: async (data: CreateScriptBancoDadosRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      databaseConnectionId: data.conexaoBancoDadosId,
      name: data.nome,
      description: data.descricao || undefined,
      script: data.script,
    });

    return {
      ...response,
      data: response.data ? mapScriptBancoDados(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdateScriptBancoDadosRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      databaseConnectionId: data.conexaoBancoDadosId,
      name: data.nome,
      description: data.descricao || undefined,
      script: data.script,
    });

    return {
      ...response,
      data: response.data ? mapScriptBancoDados(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
