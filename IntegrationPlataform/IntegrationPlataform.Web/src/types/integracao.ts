import type { CategoriaIntegracao as CategoriaIntegracaoEntity } from './categoriaIntegracao';

export interface Integracao {
  id: number;
  identificador: string;
  nome: string;
  descricao?: string;
  categoria?: CategoriaIntegracaoEntity;
  ativo: boolean;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface CreateIntegracaoRequest {
  identificador: string;
  nome: string;
  descricao?: string;
  categoriaId?: number;
  ativo: boolean;
}

export interface UpdateIntegracaoRequest {
  identificador: string;
  nome: string;
  descricao?: string;
  categoriaId?: number;
  ativo: boolean;
}

export interface IntegracaoExportModel {
  identificador: string;
  nome: string;
  descricao?: string;
  atributos: IntegracaoAtributoExportModel[];
  chamadas: ChamadaApiExportModel[];
  funcoesJs: FuncaoJsExportModel[];
  pipelines: PipelineExportModel[];
}

export interface IntegracaoAtributoExportModel {
  campo: string;
  label: string;
  descricao?: string;
  placeholder?: string;
  tipo: number;
  valorPadrao?: string;
  obrigatorio: boolean;
  ordem: number;
  grupo?: string;
  sensivel: boolean;
}

export interface ChamadaApiExportModel {
  nome: string;
  descricao?: string;
  metodo: number;
  url: string;
  headersTemplate?: string;
  bodyTemplate?: string;
}

export interface FuncaoJsExportModel {
  nome: string;
  descricao?: string;
  codigo: string;
}

export interface PipelineExportModel {
  identificador: string;
  nome: string;
  descricao?: string;
  gatilho: number;
  ativo: boolean;
  etapas: PipelineEtapaExportModel[];
}

export interface PipelineEtapaExportModel {
  ordem: number;
  nome: string;
  tipo: number;
  chamadaApiNome?: string;
  funcaoJsNome?: string;
  aoErro: number;
  ativo: boolean;
}
