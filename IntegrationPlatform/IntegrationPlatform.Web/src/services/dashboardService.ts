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
    queuePending: number;
    averageDurationTodayMs?: number;
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
  queueByStatus: Array<{
    status: number;
    count: number;
  }>;
  topConnectors: Array<{
    connector: string;
    executionCount: number;
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
          filaPendente: 0,
        },
        execucoesMensais: [],
        execucoesRecentes: [],
        filaPorStatus: [],
        topConectores: [],
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
        filaPendente: data.kpis.queuePending,
        duracaoMediaHojeMs: data.kpis.averageDurationTodayMs,
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
      filaPorStatus: data.queueByStatus.map((item) => ({
        status: item.status,
        count: item.count,
      })),
      topConectores: data.topConnectors.map((item) => ({
        conector: item.connector,
        executionCount: item.executionCount,
      })),
    };
  },
};
