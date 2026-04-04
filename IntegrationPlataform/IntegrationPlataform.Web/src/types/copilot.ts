import type { TipoEtapa } from './pipeline';

export interface CopilotPlanRequest {
  prompt: string;
  integracaoId?: number;
  conectorId?: number;
  pipelineId?: number;
}

export interface CopilotPipelineEtapaDraft {
  ordem: number;
  nome: string;
  tipo: TipoEtapa;
}

export interface CopilotDraft {
  integracao: {
    nome: string;
    identificador: string;
    descricao?: string;
  };
  conector: {
    nome: string;
  };
  pipeline: {
    nome: string;
    identificador: string;
    descricao?: string;
  };
  etapas: CopilotPipelineEtapaDraft[];
}

export interface CopilotPlanResponse {
  summary: string;
  draft: CopilotDraft;
  missingFields: string[];
  warnings: string[];
}
