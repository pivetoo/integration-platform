import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { Referencia, CreateReferenciaRequest, UpdateReferenciaRequest } from '../types/referencia';

const BASE_URL = '/referencia';

export const referenciaService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Referencia>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['entidade', 'idInterno', 'idExterno']);
    }

    oDataParams.$select = 'Id,Entidade,IdInterno,IdExterno,CreatedAt,UpdatedAt';
    oDataParams.$expand = 'Conector($select=Id,Nome)';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<Referencia[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<Referencia>(response.data, params);
  },

  getById: (id: number) => httpClient.get<Referencia>(`${BASE_URL}/${id}`),

  getByConector: (conectorId: number) =>
    httpClient.get<Referencia[]>(`${BASE_URL}/conector/${conectorId}`),

  create: (data: CreateReferenciaRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateReferenciaRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
