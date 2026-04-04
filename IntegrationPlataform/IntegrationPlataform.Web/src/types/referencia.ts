import type { Conector } from './conector';

export interface Referencia {
  id: number;
  conector: Conector;
  entidade: string;
  idInterno: string;
  idExterno: string;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface CreateReferenciaRequest {
  conectorId: number;
  entidade: string;
  idInterno: string;
  idExterno: string;
}

export interface UpdateReferenciaRequest {
  entidade: string;
  idInterno: string;
  idExterno: string;
}
