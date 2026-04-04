import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { ChamadaApi, CreateChamadaApiRequest, UpdateChamadaApiRequest } from '../types/chamadaApi';

const BASE_URL = '/chamadaapi';

export const chamadaApiService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<ChamadaApi>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['nome']);
    }

    oDataParams.$select = 'Id,Nome,Descricao,Metodo,Url,CreatedAt,UpdatedAt';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<ChamadaApi[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<ChamadaApi>(response.data, params);
  },

  getById: (id: number) =>
    httpClient.get<ChamadaApi>(`${BASE_URL}/${id}`),

  create: (data: CreateChamadaApiRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateChamadaApiRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
