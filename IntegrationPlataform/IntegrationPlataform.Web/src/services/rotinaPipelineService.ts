import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { RotinaPipeline, CreateRotinaPipelineRequest, UpdateRotinaPipelineRequest } from '../types/rotinaPipeline';

const BASE_URL = '/rotinapipeline';

export const rotinaPipelineService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<RotinaPipeline>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    oDataParams.$select = 'Id,Ativo,IntervaloMinutos,PayloadPadrao,UltimaExecucao,ProximaExecucao,CreatedAt,UpdatedAt';
    oDataParams.$expand = 'Conector($select=Id,Nome),Pipeline($select=Id,Nome)';
    oDataParams.$orderby = 'CreatedAt desc';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<RotinaPipeline[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<RotinaPipeline>(response.data, params);
  },

  create: (data: CreateRotinaPipelineRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateRotinaPipelineRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
