import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { ScriptBancoDados, CreateScriptBancoDadosRequest, UpdateScriptBancoDadosRequest } from '../types/scriptBancoDados';

const BASE_URL = '/ScriptBancoDados';

export const scriptBancoDadosService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<ScriptBancoDados>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['nome']);
    }

    oDataParams.$select = 'Id,Nome,Descricao,Script,CreatedAt,UpdatedAt';
    oDataParams.$expand = 'ConexaoBancoDados($select=Id,Nome)';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<ScriptBancoDados[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<ScriptBancoDados>(response.data, params);
  },

  getById: (id: number) => httpClient.get<ScriptBancoDados>(`${BASE_URL}/${id}`),

  create: (data: CreateScriptBancoDadosRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateScriptBancoDadosRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
