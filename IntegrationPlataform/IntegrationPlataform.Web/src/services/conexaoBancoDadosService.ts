import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { ConexaoBancoDados, CreateConexaoBancoDadosRequest } from '../types/conexaoBancoDados';

const BASE_URL = '/ConexaoBancoDados';

export interface UpdateConexaoBancoDadosRequest {
  nome: string;
  tipo: number;
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
}

export const conexaoBancoDadosService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<ConexaoBancoDados>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['nome', 'host', 'database']);
    }

    oDataParams.$select = 'Id,Nome,Tipo,Host,Port,Database,Username,CreatedAt,UpdatedAt';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<ConexaoBancoDados[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<ConexaoBancoDados>(response.data, params);
  },

  getById: (id: number) =>
    httpClient.get<ConexaoBancoDados>(`${BASE_URL}/${id}`),

  create: (data: CreateConexaoBancoDadosRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateConexaoBancoDadosRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),

  testar: (data: CreateConexaoBancoDadosRequest) =>
    httpClient.post<{ message: string }>(`${BASE_URL}/testar`, data),
};
