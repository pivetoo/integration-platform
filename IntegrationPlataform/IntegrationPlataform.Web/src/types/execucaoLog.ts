import type { PipelineEtapa } from './pipeline';

export const NivelLog = {
  Debug: 0,
  Info: 1,
  Warning: 2,
  Error: 3,
} as const;

export type NivelLog = typeof NivelLog[keyof typeof NivelLog];

export const NivelLogLabels: Record<NivelLog, string> = {
  [NivelLog.Debug]: 'Debug',
  [NivelLog.Info]: 'Info',
  [NivelLog.Warning]: 'Warning',
  [NivelLog.Error]: 'Error',
};

export interface ExecucaoLog {
  id: number;
  execucaoId: number;
  pipelineEtapa?: PipelineEtapa;
  nivel: NivelLog;
  mensagem: string;
  contexto?: string;
  requisicao?: string;
  resposta?: string;
  statusHttpCode?: number;
  duracao?: number;
  criadoEm: string;
}
