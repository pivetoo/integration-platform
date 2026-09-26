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

export interface FilaPorStatus {
  status: number;
  count: number;
}

export interface DashboardData {
  kpis: DashboardKpis;
  execucoesMensais: ExecucaoMensal[];
  filaPorStatus: FilaPorStatus[];
}
