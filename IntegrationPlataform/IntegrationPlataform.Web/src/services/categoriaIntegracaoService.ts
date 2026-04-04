import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { CategoriaIntegracao, CreateCategoriaIntegracaoRequest, UpdateCategoriaIntegracaoRequest } from '../types/categoriaIntegracao';

const BASE_URL = '/categoriaintegracao';

export const categoriaIntegracaoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<CategoriaIntegracao>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['nome']);
    }

    oDataParams.$select = 'Id,Nome,Descricao,Ativo';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<CategoriaIntegracao[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<CategoriaIntegracao>(response.data, params);
  },

  getAtivas: () =>
    httpClient.get<CategoriaIntegracao[]>(`${BASE_URL}/GetAtivas`),

  getById: (id: number) => httpClient.get<CategoriaIntegracao>(`${BASE_URL}/${id}`),

  create: (data: CreateCategoriaIntegracaoRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateCategoriaIntegracaoRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
