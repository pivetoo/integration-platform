import type { Integration } from './integration';

export interface Conector {
  id: number;
  systemApplicationId?: string;
  integrationId: number;
  integration?: Integration;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateConectorRequest {
  systemApplicationId?: string;
  integrationId: number;
  name: string;
  isActive: boolean;
}

export interface UpdateConectorRequest {
  systemApplicationId?: string;
  integrationId: number;
  name: string;
  isActive: boolean;
}
