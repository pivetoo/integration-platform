import { buildPaginationQuery, httpClient } from 'archon-ui';
import type { PaginatedResult, PaginationParams } from '../types/pagination';
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
import type { Conector } from '../types/conector';
import type { Pipeline } from '../types/pipeline';

const BASE_URL = '/Executions';

function mapConector(item: any): Conector | undefined {
  if (!item) {
    return undefined;
  }

  return {
    id: item.id,
    sistemaId: item.systemApplicationId,
    integracaoId: item.integrationId,
    integracao: item.integration ? {
      id: item.integration.id,
      identificador: item.integration.identifier,
      nome: item.integration.name,
      descricao: item.integration.description,
      categoriaId: item.integration.integrationCategoryId,
      categoria: item.integration.integrationCategory ? {
        id: item.integration.integrationCategory.id,
        nome: item.integration.integrationCategory.name,
        descricao: item.integration.integrationCategory.description,
        ativo: item.integration.integrationCategory.isActive,
        criadoEm: item.integration.integrationCategory.createdAt,
        ultimaAlteracao: item.integration.integrationCategory.updatedAt,
      } : undefined,
      ativo: item.integration.isActive,
      criadoEm: item.integration.createdAt,
      ultimaAlteracao: item.integration.updatedAt,
    } : undefined,
    nome: item.name,
    ativo: item.isActive,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

function mapPipeline(item: any): Pipeline | undefined {
  if (!item) {
    return undefined;
  }

  return {
    id: item.id,
    integracaoId: item.integrationId,
    integracao: item.integration ? {
      id: item.integration.id,
      identificador: item.integration.identifier,
      nome: item.integration.name,
      descricao: item.integration.description,
      categoriaId: item.integration.integrationCategoryId,
      categoria: item.integration.integrationCategory ? {
        id: item.integration.integrationCategory.id,
        nome: item.integration.integrationCategory.name,
        descricao: item.integration.integrationCategory.description,
        ativo: item.integration.integrationCategory.isActive,
        criadoEm: item.integration.integrationCategory.createdAt,
        ultimaAlteracao: item.integration.integrationCategory.updatedAt,
      } : undefined,
      ativo: item.integration.isActive,
      criadoEm: item.integration.createdAt,
      ultimaAlteracao: item.integration.updatedAt,
    } : undefined,
    identificador: item.identifier,
    nome: item.name,
    descricao: item.description,
    ativo: item.isActive,
    criadoEm: item.createdAt,
    ultimaAlteracao: item.updatedAt,
  };
}

function mapExecucao(item: any): Execucao {
  return {
    id: item.id,
    tipo: item.type,
    conector: mapConector(item.connector),
    pipeline: mapPipeline(item.pipeline),
    status: item.status,
    dadosEntrada: item.inputData,
    dadosSaida: item.outputData,
    erros: item.errors,
    iniciadoEm: item.startedAt,
    finalizadoEm: item.finishedAt,
    duracao: item.duration,
    criadoEm: item.createdAt ?? item.startedAt,
    ultimaAlteracao: item.updatedAt,
  };
}

function mapDebugPipelineResult(item: any): DebugPipelineResult {
  return {
    id: item.id,
    status: item.status,
    duracao: item.duration,
    dadosSaida: item.outputData,
    erros: item.errors,
  };
}

function mapStartDebugPipelineResult(item: any): StartDebugPipelineResult {
  return {
    debugSessionId: item.sessionId,
    execucaoId: item.executionId,
    status: item.status,
    totalEtapas: item.totalSteps,
    etapasRestantes: item.remainingSteps ?? item.totalSteps ?? 0,
    proximaEtapaId: item.nextStepId,
    proximaEtapaNome: item.nextStepName,
  };
}

function mapExecuteNextDebugStepResult(item: any): ExecuteNextDebugStepResult {
  return {
    debugSessionId: item.debugSessionId,
    execucaoId: item.executionId,
    executouEtapa: item.executedStep,
    etapaExecutadaId: item.executedStepId,
    etapaExecutadaNome: item.executedStepName,
    sucesso: item.success,
    interrompidoPorErro: item.interruptedByError,
    finalizouFluxo: item.finishedFlow,
    etapasRestantes: item.remainingSteps,
    proximaEtapaId: item.nextStepId,
    proximaEtapaNome: item.nextStepName,
    mensagem: item.message,
  };
}

export const execucaoService = {
  getAll: async (params?: PaginationParams): Promise<PaginatedResult<Execucao>> => {
    const query = buildPaginationQuery(params);
    const response = await httpClient.get<any[]>(`${BASE_URL}/Get${query}`);
    return {
      data: (response.data ?? []).map(mapExecucao),
      total: response.pagination?.totalCount ?? 0,
      page: response.pagination?.page ?? params?.page,
      pageSize: response.pagination?.pageSize ?? params?.pageSize,
    };
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapExecucao(response.data) : response.data,
    };
  },

  getByConector: async (conectorId: number) => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/connector/${conectorId}`);
    return {
      ...response,
      data: response.data?.map(mapExecucao) ?? [],
    };
  },

  getByStatus: async (status: number) => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/status/${status}`);
    return {
      ...response,
      data: response.data?.map(mapExecucao) ?? [],
    };
  },

  debug: (data: DebugPipelineRequest) =>
    httpClient.post<any>(`${BASE_URL}/debug`, {
      connectorId: data.conectorId,
      pipelineId: data.pipelineId,
      inputData: data.dadosEntrada,
      initialStepId: data.etapaInicialId,
    }).then((response) => ({
      ...response,
      data: response.data ? mapDebugPipelineResult(response.data) : response.data,
    })),

  startDebug: (data: StartDebugPipelineRequest) =>
    httpClient.post<any>(`${BASE_URL}/debug/start`, {
      connectorId: data.conectorId,
      pipelineId: data.pipelineId,
      inputData: data.dadosEntrada,
      initialStepId: data.etapaInicialId,
    }).then((response) => ({
      ...response,
      data: response.data ? mapStartDebugPipelineResult(response.data) : response.data,
    })),

  executeNextDebugStep: (data: ExecuteNextDebugStepRequest) =>
    httpClient.post<any>(`${BASE_URL}/debug/next-step`, {
      debugSessionId: data.debugSessionId,
    }).then((response) => ({
      ...response,
      data: response.data ? mapExecuteNextDebugStepResult(response.data) : response.data,
    })),

  finalizeDebug: (data: FinalizeDebugPipelineRequest) =>
    httpClient.post<any>(`${BASE_URL}/debug/finish`, {
      debugSessionId: data.debugSessionId,
    }).then((response) => ({
      ...response,
      data: response.data ? mapDebugPipelineResult(response.data) : response.data,
    })),
};
