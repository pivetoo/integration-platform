import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { FuncaoJavaScript, CreateFuncaoJavaScriptRequest, UpdateFuncaoJavaScriptRequest } from '../types/funcaoJavaScript';

const BASE_URL = '/funcaojavascript';

export const funcaoJavaScriptService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<FuncaoJavaScript>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['nome']);
    }

    oDataParams.$select = 'Id,Nome,Descricao,Codigo,CreatedAt,UpdatedAt';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<FuncaoJavaScript[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<FuncaoJavaScript>(response.data, params);
  },

  getById: (id: number) => httpClient.get<FuncaoJavaScript>(`${BASE_URL}/${id}`),

  create: (data: CreateFuncaoJavaScriptRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateFuncaoJavaScriptRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
