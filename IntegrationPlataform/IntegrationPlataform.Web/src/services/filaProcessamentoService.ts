import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type { FilaProcessamento, CreateFilaProcessamentoRequest, UpdateFilaProcessamentoRequest } from '../types/filaProcessamento';

const BASE_URL = '/filaprocessamento';

export const filaProcessamentoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<FilaProcessamento>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    oDataParams.$select = 'Id,Prioridade,Status,UltimoErro,Agendamento,IniciadoEm,FinalizadoEm,CreatedAt';
    oDataParams.$expand = 'Conector($select=Id,Nome),Pipeline($select=Id,Nome)';
    oDataParams.$orderby = 'CreatedAt desc';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<FilaProcessamento[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<FilaProcessamento>(response.data, params);
  },

  getById: (id: number) => httpClient.get<FilaProcessamento>(`${BASE_URL}/${id}`),

  getPendentes: () => httpClient.get<FilaProcessamento[]>(`${BASE_URL}/pendentes`),

  create: (data: CreateFilaProcessamentoRequest) =>
    httpClient.post<{ id: number; message: string }>(`${BASE_URL}/Create`, data),

  update: (id: number, data: UpdateFilaProcessamentoRequest) =>
    httpClient.put<{ message: string }>(`${BASE_URL}/${id}`, data),

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),
};
