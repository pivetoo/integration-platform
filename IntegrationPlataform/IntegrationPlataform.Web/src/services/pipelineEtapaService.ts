import { httpClient } from 'archon-ui';
import type { PipelineEtapa, CreatePipelineEtapaRequest, UpdatePipelineEtapaRequest } from '../types/pipeline';

const BASE_URL = '/PipelineSteps';

function mapPipelineEtapa(item: any): PipelineEtapa {
  return {
    id: item.id,
    pipeline: item.pipeline ? {
      id: item.pipeline.id,
      integracao: item.pipeline.integration,
      identificador: item.pipeline.identifier,
      nome: item.pipeline.name,
      descricao: item.pipeline.description,
      ativo: item.pipeline.isActive,
      criadoEm: item.pipeline.createdAt,
      ultimaAlteracao: item.pipeline.updatedAt,
    } : item.pipeline,
    ordem: item.order,
    nome: item.name,
    tipo: item.type,
    chamadaApi: item.apiCall ? { id: item.apiCall.id, nome: item.apiCall.name } : undefined,
    funcaoJavaScript: item.javaScriptFunction ? { id: item.javaScriptFunction.id, nome: item.javaScriptFunction.name } : undefined,
    scriptBancoDados: item.databaseScript ? { id: item.databaseScript.id, nome: item.databaseScript.name } : undefined,
    aoErro: item.errorAction,
    ativo: item.isActive,
    ignorarNoRetorno: item.ignoreOnResponse,
  };
}

export const pipelineEtapaService = {
  getByPipeline: async (pipelineId: number) => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/pipeline/${pipelineId}`);
    return response.data?.map(mapPipelineEtapa) ?? [];
  },

  getById: async (id: number) => {
    const response = await httpClient.get<any>(`${BASE_URL}/${id}`);
    return {
      ...response,
      data: response.data ? mapPipelineEtapa(response.data) : response.data,
    };
  },

  create: async (data: CreatePipelineEtapaRequest) => {
    const response = await httpClient.post<any>(`${BASE_URL}/Create`, {
      pipelineId: data.pipelineId,
      order: data.ordem,
      name: data.nome,
      type: data.tipo,
      apiCallId: data.chamadaApiId,
      javaScriptFunctionId: data.funcaoJavaScriptId,
      databaseScriptId: data.scriptBancoDadosId,
      errorAction: data.aoErro,
      ignoreOnResponse: data.ignorarNoRetorno ?? false,
    });

    return {
      ...response,
      data: response.data ? mapPipelineEtapa(response.data) : response.data,
    };
  },

  update: async (id: number, data: UpdatePipelineEtapaRequest) => {
    const response = await httpClient.put<any>(`${BASE_URL}/${id}`, {
      id,
      order: data.ordem,
      name: data.nome,
      type: data.tipo,
      apiCallId: data.chamadaApiId,
      javaScriptFunctionId: data.funcaoJavaScriptId,
      databaseScriptId: data.scriptBancoDadosId,
      errorAction: data.aoErro,
      isActive: data.ativo,
      ignoreOnResponse: data.ignorarNoRetorno ?? false,
    });

    return {
      ...response,
      data: response.data ? mapPipelineEtapa(response.data) : response.data,
    };
  },

  delete: (id: number) =>
    httpClient.delete<{ message: string }>(`${BASE_URL}/${id}`),

  reorder: (etapas: Array<{ id: number; ordem: number }>) =>
    httpClient.post<{ message: string }>(`${BASE_URL}/Reorder`, { etapas }),
};
