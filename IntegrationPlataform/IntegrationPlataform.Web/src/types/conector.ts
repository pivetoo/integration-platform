import type { Integracao } from './integracao';

export interface Conector {
  id: number;
  sistemaId?: string;
  integracao: Integracao;
  nome: string;
  ativo: boolean;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface CreateConectorRequest {
  sistemaId?: string;
  integracaoId: number;
  nome: string;
  ativo: boolean;
}

export interface UpdateConectorRequest {
  sistemaId?: string;
  integracaoId: number;
  nome: string;
  ativo: boolean;
}
