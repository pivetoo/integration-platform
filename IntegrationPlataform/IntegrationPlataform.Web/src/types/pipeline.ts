import type { Integracao } from './integracao';

export const TipoEtapa = {
  RequisicaoHttp: 0,
  FuncaoJavaScript: 1,
  ExecutarScript: 2,
} as const;

export type TipoEtapa = typeof TipoEtapa[keyof typeof TipoEtapa];

export const TipoEtapaLabels: Record<TipoEtapa, string> = {
  [TipoEtapa.RequisicaoHttp]: 'Requisição HTTP',
  [TipoEtapa.FuncaoJavaScript]: 'Função JavaScript',
  [TipoEtapa.ExecutarScript]: 'Executar Script SQL',
};

export const AcaoErro = {
  Parar: 0,
  Continuar: 1,
} as const;

export type AcaoErro = typeof AcaoErro[keyof typeof AcaoErro];

export const AcaoErroLabels: Record<AcaoErro, string> = {
  [AcaoErro.Parar]: 'Parar',
  [AcaoErro.Continuar]: 'Continuar',
};

export interface Pipeline {
  id: number;
  integracao: Integracao;
  identificador: string;
  nome: string;
  descricao?: string;
  ativo: boolean;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface PipelineEtapa {
  id: number;
  pipeline: Pipeline;
  ordem: number;
  nome: string;
  tipo: TipoEtapa;
  chamadaApi?: { id: number; nome: string };
  funcaoJavaScript?: { id: number; nome: string };
  scriptBancoDados?: { id: number; nome: string };
  aoErro: AcaoErro;
  ativo: boolean;
  ignorarNoRetorno?: boolean;
}

export interface CreatePipelineRequest {
  integracaoId: number;
  identificador: string;
  nome: string;
  descricao?: string;
  ativo: boolean;
}

export interface UpdatePipelineRequest {
  integracaoId: number;
  identificador: string;
  nome: string;
  descricao?: string;
  ativo: boolean;
}

export interface CreatePipelineEtapaRequest {
  pipelineId: number;
  ordem: number;
  nome: string;
  tipo: TipoEtapa;
  chamadaApiId?: number;
  funcaoJavaScriptId?: number;
  scriptBancoDadosId?: number;
  aoErro: AcaoErro;
  ativo: boolean;
  ignorarNoRetorno?: boolean;
}

export interface UpdatePipelineEtapaRequest {
  pipelineId: number;
  ordem: number;
  nome: string;
  tipo: TipoEtapa;
  chamadaApiId?: number;
  funcaoJavaScriptId?: number;
  scriptBancoDadosId?: number;
  aoErro: AcaoErro;
  ativo: boolean;
  ignorarNoRetorno?: boolean;
}
