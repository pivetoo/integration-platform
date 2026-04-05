import type { Conector } from './conector';

export interface Reference {
  id: number;
  connector: Conector;
  entity: string;
  internalId: string;
  externalId: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateReferenceRequest {
  connectorId: number;
  entity: string;
  internalId: string;
  externalId: string;
}

export interface UpdateReferenceRequest {
  entity: string;
  internalId: string;
  externalId: string;
}
