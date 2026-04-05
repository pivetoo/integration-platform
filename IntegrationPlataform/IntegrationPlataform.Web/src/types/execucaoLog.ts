import type { AcaoErro, TipoEtapa } from './pipeline';

export const NivelLog = {
  Debug: 1,
  Info: 2,
  Warning: 3,
  Error: 4,
} as const;

export type NivelLog = typeof NivelLog[keyof typeof NivelLog];

export const NivelLogLabels: Record<NivelLog, string> = {
  [NivelLog.Debug]: 'Debug',
  [NivelLog.Info]: 'Info',
  [NivelLog.Warning]: 'Warning',
  [NivelLog.Error]: 'Error',
};

export interface ExecucaoLogEtapa {
  id: number;
  ordem: number;
  nome: string;
  tipo: TipoEtapa;
  aoErro: AcaoErro;
  ativo: boolean;
  ignorarNoRetorno: boolean;
}

export interface ExecucaoLog {
  id: number;
  execucaoId: number;
  pipelineEtapa?: ExecucaoLogEtapa;
  nivel: NivelLog;
  mensagem: string;
  contexto?: string;
  requisicao?: string;
  resposta?: string;
  statusHttpCode?: number;
  duracao?: number;
  criadoEm: string;
}
