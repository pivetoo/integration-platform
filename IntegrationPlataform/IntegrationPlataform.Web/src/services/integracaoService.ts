import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { Integracao, CreateIntegracaoRequest, UpdateIntegracaoRequest, IntegracaoExportModel } from '../types/integracao';

const BASE_URL = '/integracao';

export const integracaoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Integracao>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    oDataParams.$select = 'Id,Identificador,Nome,Descricao,Categoria,Ativo,CreatedAt,UpdatedAt';

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['identificador', 'nome']);
    }

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<Integracao[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<Integracao>(response.data, params);
  },

  getAtivas: () => httpClient.get<Integracao[]>(`${BASE_URL}/ativas`),

  getById: (id: number) => httpClient.get<Integracao>(`${BASE_URL}/${id}`),

  create: (data: CreateIntegracaoRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateIntegracaoRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),

  exportar: (id: number) =>
    httpClient.get<IntegracaoExportModel>(`${BASE_URL}/export/${id}`),

  importar: (data: IntegracaoExportModel) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/import`, data),
};
