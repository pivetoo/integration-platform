import { httpClient } from 'archon-ui';
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
    const data = response.data;

    if (!data) {
      return {
        kpis: {
          integracoesAtivas: 0,
          conectoresAtivos: 0,
          pipelinesAtivos: 0,
          execucoesHoje: 0,
          taxaSucesso: 0,
          errosHoje: 0,
        },
        execucoesMensais: [],
        execucoesRecentes: [],
      };
    }

    return {
      kpis: {
        integracoesAtivas: data.kpis.activeIntegrations,
        conectoresAtivos: data.kpis.activeConnectors,
        pipelinesAtivos: data.kpis.activePipelines,
        execucoesHoje: data.kpis.executionsToday,
        taxaSucesso: data.kpis.successRate,
        errosHoje: data.kpis.errorsToday,
      },
      execucoesMensais: data.monthlyExecutions.map((item) => ({
        mes: item.month,
        sucesso: item.success,
        erro: item.error,
      })),
      execucoesRecentes: data.recentExecutions.map((item) => ({
        id: item.id,
        pipeline: item.pipeline,
        conector: item.connector,
        status: item.status,
        duration: item.duration,
        startedAt: item.startedAt,
      })),
    };
  },
};
