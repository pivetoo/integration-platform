export interface DashboardKpis {
  integracoesAtivas: number;
  conectoresAtivos: number;
  pipelinesAtivos: number;
  execucoesHoje: number;
  taxaSucesso: number;
  errosHoje: number;
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

export interface DashboardData {
  kpis: DashboardKpis;
  execucoesMensais: ExecucaoMensal[];
  execucoesRecentes: ExecucaoRecente[];
}
