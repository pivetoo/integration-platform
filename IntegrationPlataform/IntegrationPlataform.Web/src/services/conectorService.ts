import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { Conector, CreateConectorRequest, UpdateConectorRequest } from '../types/conector';

const BASE_URL = '/conector';

export const conectorService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Conector>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['nome']);
    }

    oDataParams.$select = 'Id,SistemaId,Nome,Ativo,CreatedAt,UpdatedAt';
    oDataParams.$expand = 'Integracao($select=Id,Nome)';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<Conector[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<Conector>(response.data, params);
  },

  getByIntegracao: (integracaoId: number) =>
    httpClient.get<Conector[]>(`${BASE_URL}/integracao/${integracaoId}`),

  getAtivos: () => httpClient.get<Conector[]>(`${BASE_URL}/ativos`),

  getById: (id: number) => httpClient.get<Conector>(`${BASE_URL}/${id}`),

  create: (data: CreateConectorRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateConectorRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
