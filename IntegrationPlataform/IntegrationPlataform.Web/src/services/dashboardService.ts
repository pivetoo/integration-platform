import { httpClient } from 'd-rts';
import type { DashboardData } from '../types/dashboard';

const BASE_URL = '/dashboard';

interface DashboardApiData {
  kpis: {
    activeIntegrations: number;
    activeConnectors: number;
    activePipelines: number;
    executionsToday: number;
    successRate: number;
    errorsToday: number;
  };
  monthlyExecutions: Array<{
    month: string;
    success: number;
    error: number;
  }>;
  recentExecutions: Array<{
    id: number;
    pipeline: string;
    connector: string;
    status: number;
    duration?: number;
    startedAt: string;
  }>;
}

export const dashboardService = {
  async getData(): Promise<DashboardData> {
    const response = await httpClient.get<DashboardApiData>(`${BASE_URL}/GetDashboardData`);

    return {
      kpis: {
        integracoesAtivas: response.kpis.activeIntegrations,
        conectoresAtivos: response.kpis.activeConnectors,
        pipelinesAtivos: response.kpis.activePipelines,
        execucoesHoje: response.kpis.executionsToday,
        taxaSucesso: response.kpis.successRate,
        errosHoje: response.kpis.errorsToday,
      },
      execucoesMensais: response.monthlyExecutions.map((item) => ({
        mes: item.month,
        sucesso: item.success,
        erro: item.error,
      })),
      execucoesRecentes: response.recentExecutions.map((item) => ({
        id: item.id,
        pipeline: item.pipeline,
        conector: item.connector,
        status: item.status,
        duracao: item.duration,
        iniciadoEm: item.startedAt,
      })),
    };
  },
};
