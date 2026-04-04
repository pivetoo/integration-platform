import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { Pipeline, CreatePipelineRequest, UpdatePipelineRequest } from '../types/pipeline';

const BASE_URL = '/pipeline';

export const pipelineService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Pipeline>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    if (params?.search) {
      oDataParams.$filter = ODataHelper.createSearchFilter(params.search, ['identificador', 'nome']);
    }

    oDataParams.$select = 'Id,Identificador,Nome,Descricao,Ativo,CreatedAt,UpdatedAt';
    oDataParams.$expand = 'Integracao($select=Id,Nome)';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<Pipeline[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<Pipeline>(response.data, params);
  },

  getByIntegracao: (integracaoId: number) =>
    httpClient.get<Pipeline[]>(`${BASE_URL}/integracao/${integracaoId}`),

  getAtivos: () => httpClient.get<Pipeline[]>(`${BASE_URL}/ativos`),

  getById: (id: number) => httpClient.get<Pipeline>(`${BASE_URL}/${id}`),

  create: (data: CreatePipelineRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdatePipelineRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
