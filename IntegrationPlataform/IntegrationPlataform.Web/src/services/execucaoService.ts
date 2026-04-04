import { httpClient, ODataHelper } from 'd-rts';
import type { PaginatedResult, PaginationParams } from 'd-rts';
import type {
  DebugPipelineRequest,
  DebugPipelineResult,
  Execucao,
  ExecuteNextDebugStepRequest,
  ExecuteNextDebugStepResult,
  FinalizeDebugPipelineRequest,
  StartDebugPipelineRequest,
  StartDebugPipelineResult
} from '../types/execucao';

const BASE_URL = '/execucao';

export const execucaoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Execucao>> => {
    const oDataParams = params ? ODataHelper.fromPaginationParams(params) : { $count: true };

    oDataParams.$select = 'Id,Tipo,Status,IniciadoEm,FinalizadoEm,Duracao,CreatedAt';
    oDataParams.$expand = 'Conector($select=Id,Nome),Pipeline($select=Id,Nome)';
    oDataParams.$orderby = 'IniciadoEm desc';

    const query = ODataHelper.buildQuery(oDataParams);
    const response = await httpClient.get<Execucao[]>(`${BASE_URL}/GetAll${query}`);
    return ODataHelper.processResponse<Execucao>(response.data, params);
  },

  getById: (id: number) => httpClient.get<Execucao>(`${BASE_URL}/${id}`),

  getByConector: (conectorId: number) =>
    httpClient.get<Execucao[]>(`${BASE_URL}/conector/${conectorId}`),

  getByStatus: (status: number) =>
    httpClient.get<Execucao[]>(`${BASE_URL}/status/${status}`),

  debug: (data: DebugPipelineRequest) =>
    httpClient.post<DebugPipelineResult>(`${BASE_URL}/debug`, data),

  startDebug: (data: StartDebugPipelineRequest) =>
    httpClient.post<StartDebugPipelineResult>(`${BASE_URL}/debug/iniciar`, data),

  executeNextDebugStep: (data: ExecuteNextDebugStepRequest) =>
    httpClient.post<ExecuteNextDebugStepResult>(`${BASE_URL}/debug/proxima-etapa`, data),

  finalizeDebug: (data: FinalizeDebugPipelineRequest) =>
    httpClient.post<DebugPipelineResult>(`${BASE_URL}/debug/finalizar`, data),
};
