import type { ConexaoBancoDados } from './conexaoBancoDados';

export interface ScriptBancoDados {
  id: number;
  conexaoBancoDadosId: number;
  conexaoBancoDados?: ConexaoBancoDados;
  nome: string;
  descricao: string | null;
  script: string;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface CreateScriptBancoDadosRequest {
  conexaoBancoDadosId: number;
  nome: string;
  descricao: string;
  script: string;
}

export interface UpdateScriptBancoDadosRequest {
  conexaoBancoDadosId: number;
  nome: string;
  descricao: string;
  script: string;
}
