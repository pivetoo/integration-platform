import { httpClient } from 'archon-ui';
import type { ExecucaoLog } from '../types/execucaoLog';

const BASE_URL = '/ExecutionLogs';

function mapExecucaoLog(item: any): ExecucaoLog {
  return {
    id: item.id,
    execucaoId: item.executionId,
    pipelineEtapa: item.pipelineStep ? {
      id: item.pipelineStep.id,
      ordem: item.pipelineStep.order,
      nome: item.pipelineStep.name,
      tipo: item.pipelineStep.type,
      aoErro: item.pipelineStep.errorAction,
      ativo: item.pipelineStep.isActive,
      ignorarNoRetorno: item.pipelineStep.ignoreOnResponse,
    } : undefined,
    nivel: item.level,
    mensagem: item.message,
    contexto: item.context,
    requisicao: item.request,
    resposta: item.response,
    statusHttpCode: item.httpStatusCode,
    duracao: item.duration,
    criadoEm: item.createdAt,
  };
}

export const execucaoLogService = {
  getByExecucao: async (execucaoId: number) => {
    const response = await httpClient.get<any[]>(`${BASE_URL}/execution/${execucaoId}`);
    return {
      ...response,
      data: response.data?.map(mapExecucaoLog) ?? [],
    };
  },
};
