import type { Conector } from './conector';
import type { Pipeline } from './pipeline';

export interface RotinaPipeline {
  id: number;
  conector: Conector;
  pipeline: Pipeline;
  ativo: boolean;
  intervaloMinutos: number;
  payloadPadrao?: string;
  ultimaExecucao?: string;
  proximaExecucao?: string;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface CreateRotinaPipelineRequest {
  conectorId: number;
  pipelineId: number;
  ativo: boolean;
  intervaloMinutos: number;
  payloadPadrao?: string;
  proximaExecucao?: string;
}

export interface UpdateRotinaPipelineRequest {
  ativo: boolean;
  intervaloMinutos: number;
  payloadPadrao?: string;
  proximaExecucao?: string;
}
