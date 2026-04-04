import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
import type { ConexaoBancoDados, CreateConexaoBancoDadosRequest } from '../types/conexaoBancoDados';

const BASE_URL = '/DatabaseConnections';

export interface UpdateConexaoBancoDadosRequest {
  nome: string;
  tipo: number;
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
}

function mapConexaoBancoDados(item: any): ConexaoBancoDados {
  return {
    id: item.id,
    nome: item.name,
    tipo: item.type,
    host: item.host,
    port: item.port,
    database: item.database,
    username: item.username,
    password: item.password,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

export const conexaoBancoDadosService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<ConexaoBancoDados>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);

    return {
      data: (response.data ?? []).map(mapConexaoBancoDados),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapConexaoBancoDados(response.data) : response.data,
    };
  },

  create: async (data: CreateConexaoBancoDadosRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      name: data.nome,
      type: data.tipo,
      host: data.host,
      port: data.port,
      database: data.database,
      username: data.username,
      password: data.password,
    });

    return {
      ...response,
      data: response.data ? mapConexaoBancoDados(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdateConexaoBancoDadosRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      name: data.nome,
      type: data.tipo,
      host: data.host,
      port: data.port,
      database: data.database,
      username: data.username,
      password: data.password,
    });

    return {
      ...response,
      data: response.data ? mapConexaoBancoDados(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),

  testar: (data: CreateConexaoBancoDadosRequest) =>
    httpClient.post<{ message: string }>(`${BASE_URL}/test`, {
      name: data.nome,
      type: data.tipo,
      host: data.host,
      port: data.port,
      database: data.database,
      username: data.username,
      password: data.password,
    }),
};
