export interface DashboardKpis {
  integracoesAtivas: number;
  conectoresAtivos: number;
  pipelinesAtivos: number;
  execucoesHoje: number;
  taxaSucesso: number;
  errosHoje: number;
  filaPendente: number;
  duracaoMediaHojeMs?: number;
}

export interface ExecucaoMensal {
  mes: string;
  sucesso: number;
  erro: number;
}

export interface ExecucaoRecente {
  id: number;
  pipeline: string;
  conector: string;
  status: number;
  duration?: number;
  startedAt: string;
}

export interface FilaPorStatus {
  status: number;
  count: number;
}

export interface ConectorPorExecucoes {
  conector: string;
  executionCount: number;
}

export interface DashboardData {
  kpis: DashboardKpis;
  execucoesMensais: ExecucaoMensal[];
  execucoesRecentes: ExecucaoRecente[];
  filaPorStatus: FilaPorStatus[];
  topConectores: ConectorPorExecucoes[];
}
